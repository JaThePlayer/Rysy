namespace Rysy.Helpers;

public static class Easing {
    public delegate float Easer(float t);

    public static readonly Easer YoYo = t => {
        if (t <= 0.5f) {
            return t * 2f;
        }
        return 1f - (t - 0.5f) * 2f;
    };

    public static readonly Easer CubeIn = t => t * t * t;
    public static readonly Easer ExpoIn = t => float.Pow(2.0f, 10.0f * (t - 1.0f));
    
    public static readonly Easer BounceIn = t =>
    {
        var invT = 1.0f - t;
        if (invT < 0.3636363744735718)
        {
            return (float)(1.0 - 7.562499549239894 * invT * invT);
        }
        if (invT < 0.7272727489471436)
        {
            return (float)(1.0 - (7.562499549239894 * (invT - 0.5454545617103577) * (invT - 0.5454545617103577) + 0.75));
        }
        return invT < 0.9090909361839294 ? (float)(1.0 - (7.562499549239894 * (invT - 0.8181818127632141) * (invT - 0.8181818127632141) + 0.9375)) : ((float)(1.0 - (7.562499549239894 * (invT - 0.9545454382896423) * (invT - 0.9545454382896423) + 63.0 / 64.0)));
    };
}
