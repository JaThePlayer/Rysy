namespace Rysy.Helpers;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A MemoryStream, but allocating the underlying buffer from an array pool.
/// </summary>
public sealed class PooledMemoryStream : Stream {
    internal struct CachedCompletedInt32Task {
        private Task<int>? _task;

        /// <summary>Gets a completed <see cref="Task{Int32}"/> whose result is <paramref name="result"/>.</summary>
        /// <remarks>This method will try to return an already cached task if available.</remarks>
        /// <param name="result">The result value for which a <see cref="Task{Int32}"/> is needed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> GetTask(int result) {
            if (_task is Task<int> task) {
                Debug.Assert(task.IsCompletedSuccessfully, "Expected that a stored last task completed successfully");
                if (task.Result == result) {
                    return task;
                }
            }

            return _task = Task.FromResult(result);
        }
    }

    private readonly ArrayPool<byte> _pool;
    private byte[] _buffer;

    private readonly int _origin; // For user-provided arrays, start at this origin
    private int _position; // read/write head.
    private int _length; // Number of bytes within the memory stream
    private int _capacity; // length of usable portion of buffer for stream
    // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

    private bool _expandable; // User-provided buffers aren't expandable.
    private bool _writable; // Can user write to this stream?
    private readonly bool _exposable; // Whether the array can be returned to the user.
    private bool _isOpen; // Is this stream open or closed?

    private CachedCompletedInt32Task _lastReadTask; // The last successful task returned from ReadAsync

    private const int MemStreamMaxLength = int.MaxValue;

    public PooledMemoryStream()
        : this(0) {
    }

    public PooledMemoryStream(int capacity, ArrayPool<byte>? pool = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _pool = pool ?? ArrayPool<byte>.Shared;
        _buffer = capacity != 0 ? _pool.Rent(capacity) : [];
        _capacity = _buffer.Length;
        _expandable = true;
        _writable = true;
        _exposable = true;
        _isOpen = true;
    }

    public override bool CanRead => _isOpen;

    public override bool CanSeek => _isOpen;

    public override bool CanWrite => _writable;

    private void EnsureNotClosed() {
        ObjectDisposedException.ThrowIf(!_isOpen, "Stream is closed");
    }

    private void EnsureWriteable() {
        ObjectDisposedException.ThrowIf(!CanWrite, "Stream is not writeable");
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _isOpen = false;
            _writable = false;
            _expandable = false;
            _lastReadTask = default;
        }

        _pool.Return(_buffer);
        _buffer = [];
    }

    // returns a bool saying whether we allocated a new array.
    private bool EnsureCapacity(int value) {
        // Check for overflow
        if (value < 0)
            throw new IOException("Stream became too long!");

        if (value > _capacity) {
            int newCapacity = Math.Max(value, 256);

            // We are ok with this overflowing since the next statement will deal
            // with the cases where _capacity*2 overflows.
            if (newCapacity < _capacity * 2) {
                newCapacity = _capacity * 2;
            }

            // We want to expand the array up to Array.MaxLength.
            // And we want to give the user the value that they asked for
            if ((uint) (_capacity * 2) > Array.MaxLength) {
                newCapacity = Math.Max(value, Array.MaxLength);
            }

            Capacity = newCapacity;
            return true;
        }

        return false;
    }

    public override void Flush() {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try {
            Flush();
            return Task.CompletedTask;
        } catch (Exception ex) {
            return Task.FromException(ex);
        }
    }

    // Gets & sets the capacity (number of bytes allocated) for this stream.
    // The capacity cannot be set to a value less than the current length
    // of the stream.
    //
    public int Capacity {
        get {
            EnsureNotClosed();
            return _capacity - _origin;
        }
        set {
            // Only update the capacity if the MS is expandable and the value is different than the current capacity.
            // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
            if (value < Length)
                throw new ArgumentOutOfRangeException(nameof(value), "Tried to reduce size of a PooledMemoryStream");

            EnsureNotClosed();

            if (!_expandable && (value != Capacity))
                throw new NotSupportedException("Stream is no longer expandable");

            // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
            if (_expandable && value != _capacity) {
                if (value > 0) {
                    byte[] newBuffer = _pool.Rent(value);
                    if (_length > 0) {
                        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
                    }
                    _pool.Return(_buffer);
                    _buffer = newBuffer;
                } else {
                    _buffer = [];
                }

                _capacity = value;
            }
        }
    }

    public override long Length {
        get {
            EnsureNotClosed();
            return _length - _origin;
        }
    }

    public override long Position {
        get {
            EnsureNotClosed();
            return _position - _origin;
        }
        set {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            EnsureNotClosed();

            if (value > MemStreamMaxLength - _origin)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream length exceeded maximum");
            _position = _origin + (int) value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();

        int n = _length - _position;
        if (n > count)
            n = count;
        if (n <= 0)
            return 0;

        Debug.Assert(_position + n >= 0); // len is less than 2^31 -1.

        if (n <= 8) {
            int byteCount = n;
            while (--byteCount >= 0)
                buffer[offset + byteCount] = _buffer[_position + byteCount];
        } else
            Buffer.BlockCopy(_buffer, _position, buffer, offset, n);

        _position += n;

        return n;
    }

    public override int Read(Span<byte> buffer) {
        EnsureNotClosed();

        int n = Math.Min(_length - _position, buffer.Length);
        if (n <= 0)
            return 0;

        new Span<byte>(_buffer, _position, n).CopyTo(buffer);

        _position += n;
        return n;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation was requested, bail early
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);

        try {
            int n = Read(buffer, offset, count);
            return _lastReadTask.GetTask(n);
        } catch (OperationCanceledException oce) {
            return Task.FromCanceled<int>(oce.CancellationToken);
        } catch (Exception exception) {
            return Task.FromException<int>(exception);
        }
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        if (cancellationToken.IsCancellationRequested) {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        try {
            // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
            // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
            // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
            // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
            // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
            // MemoryStream, but if we're dealing with a type derived from MemoryStream, Read(Span<byte>) will end up delegating
            // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
            // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
            // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
            // else; if this is a concrete MemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
            // something other than an array and this is a MemoryStream-derived type that doesn't override Read(Span<byte>) will
            // it then fall back to doing the ArrayPool/copy behavior.
            return new ValueTask<int>(
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray)
                    ? Read(destinationArray.Array!, destinationArray.Offset, destinationArray.Count)
                    : Read(buffer.Span));
        } catch (OperationCanceledException oce) {
            return new ValueTask<int>(Task.FromCanceled<int>(oce.CancellationToken));
        } catch (Exception exception) {
            return ValueTask.FromException<int>(exception);
        }
    }

    public override int ReadByte() {
        EnsureNotClosed();

        if (_position >= _length)
            return -1;

        return _buffer[_position++];
    }
    
    // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
    internal int InternalEmulateRead(int count)
    {
        EnsureNotClosed();

        int n = _length - _position;
        if (n > count)
            n = count;
        if (n < 0)
            n = 0;

        Debug.Assert(_position + n >= 0);  // len is less than 2^31 -1.
        _position += n;
        return n;
    }

    public override void CopyTo(Stream destination, int bufferSize) {
        // Validate the arguments the same way Stream does for back-compat.
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        int originalPosition = _position;

        // Seek to the end of the MemoryStream.
        int remaining = InternalEmulateRead(_length - originalPosition);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (remaining > 0) {
            // Call Write() on the other Stream, using our internal buffer and avoiding any
            // intermediary allocations.
            destination.Write(_buffer, originalPosition, remaining);
        }
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
        // This implementation offers better performance compared to the base class version.

        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        // If canceled - return fast:
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        // Avoid copying data from this buffer into a temp buffer:
        // (require that InternalEmulateRead does not throw,
        // otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

        int pos = _position;
        int n = InternalEmulateRead(_length - _position);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (n == 0)
            return Task.CompletedTask;

        // If destination is not a memory stream, write there asynchronously:
        if (destination is not MemoryStream memStrDest)
            return destination.WriteAsync(_buffer, pos, n, cancellationToken);

        try {
            // If destination is a MemoryStream, CopyTo synchronously:
            memStrDest.Write(_buffer, pos, n);
            return Task.CompletedTask;
        } catch (Exception ex) {
            return Task.FromException(ex);
        }
    }

    public override long Seek(long offset, SeekOrigin loc) {
        EnsureNotClosed();

        return SeekCore(offset, loc switch {
            SeekOrigin.Begin => _origin,
            SeekOrigin.Current => _position,
            SeekOrigin.End => _length,
            _ => throw new ArgumentException("Invalid SeekOrigin")
        });
    }

    private void ThrowArgumentOutOfRangeStreamLength(string name) {
        throw new ArgumentOutOfRangeException(name, "Value exceeded stream length maximum");
    }
    
    private IOException StreamTooLong() {
        return new IOException("Stream became too long");
    }

    private long SeekCore(long offset, int loc) {
        if (offset > MemStreamMaxLength - loc)
            ThrowArgumentOutOfRangeStreamLength(nameof(offset));
        int tempPosition = unchecked(loc + (int) offset);
        if (unchecked(loc + offset) < _origin || tempPosition < _origin)
            throw new IOException("Tried to seek before starting pos");
        _position = tempPosition;

        Debug.Assert(_position >= _origin);
        return _position - _origin;
    }

    // Sets the length of the stream to a given value.  The new
    // value must be nonnegative and less than the space remaining in
    // the array, int.MaxValue - origin
    // Origin is 0 in all cases other than a MemoryStream created on
    // top of an existing array and a specific starting offset was passed
    // into the MemoryStream constructor.  The upper bounds prevents any
    // situations where a stream may be created on top of an array then
    // the stream is made longer than the maximum possible length of the
    // array (int.MaxValue).
    //
    public override void SetLength(long value) {
        if (value < 0 || value > int.MaxValue)
            ThrowArgumentOutOfRangeStreamLength(nameof(value));

        EnsureWriteable();

        // Origin wasn't publicly exposed above.
        Debug.Assert(MemStreamMaxLength ==
                     int.MaxValue); // Check parameter validation logic in this method if this fails.
        if (value > (int.MaxValue - _origin))
            ThrowArgumentOutOfRangeStreamLength(nameof(value));

        int newLength = _origin + (int) value;
        bool allocatedNewArray = EnsureCapacity(newLength);
        if (!allocatedNewArray && newLength > _length)
            Array.Clear(_buffer, _length, newLength - _length);
        _length = newLength;
        if (_position > newLength)
            _position = newLength;
    }

    public byte[] ToArray() {
        int count = _length - _origin;
        if (count == 0)
            return [];
        byte[] copy = GC.AllocateUninitializedArray<byte>(count);
        _buffer.AsSpan(_origin, count).CopyTo(copy);
        return copy;
    }

    public override void Write(byte[] buffer, int offset, int count) {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();
        EnsureWriteable();

        int i = _position + count;
        // Check for overflow
        if (i < 0)
            throw StreamTooLong();

        if (i > _length) {
            bool mustZero = _position > _length;
            if (i > _capacity) {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                Array.Clear(_buffer, _length, i - _length);
            }

            _length = i;
        }

        if ((count <= 8) && (buffer != _buffer)) {
            int byteCount = count;
            while (--byteCount >= 0) {
                _buffer[_position + byteCount] = buffer[offset + byteCount];
            }
        } else {
            Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
        }

        _position = i;
    }

    public override void Write(ReadOnlySpan<byte> buffer) {
        EnsureNotClosed();
        EnsureWriteable();

        // Check for overflow
        int i = _position + buffer.Length;
        if (i < 0)
            throw StreamTooLong();

        if (i > _length) {
            bool mustZero = _position > _length;
            if (i > _capacity) {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                Array.Clear(_buffer, _length, i - _length);
            }

            _length = i;
        }

        buffer.CopyTo(new Span<byte>(_buffer, _position, buffer.Length));
        _position = i;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation is already requested, bail early
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        } catch (OperationCanceledException oce) {
            return Task.FromCanceled(oce.CancellationToken);
        } catch (Exception exception) {
            return Task.FromException(exception);
        }
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        if (cancellationToken.IsCancellationRequested) {
            return ValueTask.FromCanceled(cancellationToken);
        }

        try {
            // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
            // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> sourceArray)) {
                Write(sourceArray.Array!, sourceArray.Offset, sourceArray.Count);
            } else {
                Write(buffer.Span);
            }

            return default;
        } catch (OperationCanceledException oce) {
            return new ValueTask(Task.FromCanceled(oce.CancellationToken));
        } catch (Exception exception) {
            return ValueTask.FromException(exception);
        }
    }

    public override void WriteByte(byte value) {
        EnsureNotClosed();
        EnsureWriteable();

        if (_position >= _length) {
            int newLength = _position + 1;
            bool mustZero = _position > _length;
            if (newLength >= _capacity) {
                bool allocatedNewArray = EnsureCapacity(newLength);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                Array.Clear(_buffer, _length, _position - _length);
            }

            _length = newLength;
        }

        _buffer[_position++] = value;
    }

    // Writes this MemoryStream to another stream.
    public void WriteTo(Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);

        EnsureNotClosed();

        stream.Write(_buffer, _origin, _length - _origin);
    }
}