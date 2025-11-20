namespace Rysy.Helpers;

public interface ISimilar {
    public bool IsSimilarTo(object other);

    public static bool Check(object? a, object? b) {
        if (a is null)
            return b is null;
        if (b is null)
            return false;

        if (a.GetType() == b.GetType()) {
            if (a is ISimilar sim)
                return sim.IsSimilarTo(b);
        } else {
            if (a is IConvertible && b is IConvertible bc) {
                try {
                    return a.Equals(bc.ToType(a.GetType(), CultureInfo.InvariantCulture));
                } catch (Exception) {
                    // ignored
                }
            }
        }

        return a.Equals(b);
    }
}

public interface ISimilar<in TSelf> : ISimilar {
    bool ISimilar.IsSimilarTo(object other) => IsSimilarTo((TSelf)other);
    
    public bool IsSimilarTo(TSelf other);
}