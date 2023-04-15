namespace Rysy.Helpers;

public static class Easing {
    public delegate float Easer(float t);

    public static readonly Easer YoYo = (float t) => {
        if (t <= 0.5f) {
            return t * 2f;
        }
        return 1f - (t - 0.5f) * 2f;
    };

}
