using MudBlazor;

namespace Web.Theme;

/// <summary>
/// Design system TicketPrime — modo claro institucional (Corporate Elite).
/// </summary>
public static class TicketPrimeTheme
{
    public const string Slate50 = "#F8FAFC";
    public const string White = "#FFFFFF";
    public const string RoyalBlue = "#2563EB";
    public const string Slate900 = "#0F172A";
    public const string Slate400 = "#94A3B8";
    public const string Slate200 = "#E2E8F0";

    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = Slate900,
            White = White,
            Background = Slate50,
            BackgroundGray = "#F1F5F9",
            Surface = White,
            DrawerBackground = White,
            DrawerText = Slate900,
            AppbarBackground = RoyalBlue,
            AppbarText = White,
            Primary = RoyalBlue,
            PrimaryContrastText = White,
            PrimaryDarken = "#1D4ED8",
            PrimaryLighten = "#3B82F6",
            Secondary = "#1E40AF",
            SecondaryContrastText = White,
            TextPrimary = Slate900,
            TextSecondary = "#475569",
            TextDisabled = Slate400,
            ActionDefault = "#64748B",
            ActionDisabled = Slate400,
            ActionDisabledBackground = "#F1F5F9",
            Divider = Slate200,
            DividerLight = "#F1F5F9",
            TableLines = Slate200,
            LinesDefault = Slate200,
            LinesInputs = "#CBD5E1",
            Dark = Slate400,
            DarkContrastText = White,
            Success = "#059669",
            Info = "#0284C7",
            Warning = "#D97706",
            Error = "#DC2626",
            HoverOpacity = 0.04,
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Roboto", "system-ui", "Segoe UI", "sans-serif" },
            },
        },
    };
}
