namespace Web.Helpers;

/// <summary>
/// Capas visuais para eventos — imagens Unsplash curadas por tipo de evento.
/// </summary>
public static class EventCoverHelper
{
    private static readonly string[] TeatroImages =
    [
        "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800&h=500&fit=crop&q=80",
        "https://images.unsplash.com/photo-1514306191717-452ec2284967?w=800&h=500&fit=crop&q=80",
        "https://images.unsplash.com/photo-1585699320791-55b4118926d7?w=800&h=500&fit=crop&q=80",
    ];

    private static readonly string[] PalestraImages =
    [
        "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&h=500&fit=crop&q=80",
        "https://images.unsplash.com/photo-1475721027785-f74eccf8e192?w=800&h=500&fit=crop&q=80",
        "https://images.unsplash.com/photo-1591115765373-5207764f72e7?w=800&h=500&fit=crop&q=80",
    ];

    public static string GetCoverUrl(int tipo, Guid eventoId) =>
        GetCoverUrl(tipo, eventoId.GetHashCode());

    public static string GetCoverUrl(int tipo, int seed)
    {
        var images = tipo == 1 ? PalestraImages : TeatroImages;
        var index = Math.Abs(seed) % images.Length;
        return images[index];
    }

    public static string GetGradient(int tipo) =>
        tipo == 1
            ? "linear-gradient(135deg, #0D9488 0%, #14B8A6 50%, #6B21A8 100%)"
            : "linear-gradient(135deg, #6B21A8 0%, #7C3AED 50%, #C9A962 100%)";
}
