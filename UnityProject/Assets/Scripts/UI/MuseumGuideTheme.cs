using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Tokens semânticos compartilhados pela interface nativa uGUI.</summary>
    public static class MuseumGuideTheme
    {
        public static readonly Color Surface = new Color(.035f, .075f, .08f, .96f);
        public static readonly Color Text = new Color(.97f, .94f, .86f);
        public static readonly Color Accent = new Color(.82f, .69f, .45f);
        public static readonly Color Control = new Color(.10f, .17f, .18f);
        public static readonly Color Focus = new Color(.28f, .37f, .36f);
        // Realce de opção ativa: mais claro que Control, ainda escuro o bastante para o texto claro continuar legível.
        public static readonly Color ActiveControl = new Color(.47f, .37f, .17f);
        public static readonly Color Scrim = new Color(0f, 0f, 0f, .55f);
        public static readonly Color WelcomeBackground = new Color(Surface.r, Surface.g, Surface.b, 1);
        public static readonly Color OnAccent = WelcomeBackground;
        public static readonly Color ActionHover = new Color(.90f, .79f, .57f);
        public static readonly Color ActionPressed = new Color(.73f, .60f, .37f);
        public static readonly Color DisabledControl = new Color(.16f, .20f, .20f);
        public const int Body = 18, Title = 28, Label = 16, Padding = 20, Gap = 12;
        // 48 = altura mínima recomendada de área de toque (48dp) na escala usada pelo CanvasScaler do projeto.
        public const float Width = 440, ButtonHeight = 48, Fade = .25f, Radius = 8;
        public const float SettingsMinWidth = 280, SettingsMaxWidth = 420, SettingsMaxHeight = 480;
        public const string FontResource = "LegacyRuntime.ttf";
        public const float ExhibitDwell = .55f, ExhibitLinger = 1.5f;
        public const float VrDwell = 1f, VrDistance = 2.4f, VrScale = .00155f;
        public const float VrWidth = 900, VrHeight = 930, VrButtonHeight = 76, VrGap = 14;
        public const int VrBody = 32, VrTitle = 40, VrButtonText = 28;
        // Opções laterais deixam livre o gesto de olhar para baixo para caminhar.
        public const float VrToolbarWidth = 280, VrToolbarHeight = 100, VrToolbarDistance = 1.82f;
        public const float VrReticleDistance = 1.3f, VrReticleScale = .0008f, VrReticleSize = 42, VrReticleDot = 7;
        public const float WalkButtonWidth = 230, WalkButtonHeight = 76;

        // A abertura usa o mesmo tema do guia, com dimensões próprias para toque em paisagem.
        public const int WelcomeTitle = 36, WelcomeCompactTitle = 30, WelcomeModeTitle = 22, WelcomeCaption = 14;
        public const float WelcomePadding = 24, WelcomePanelGap = 32, WelcomeMaxWidth = 1080;
        public const float WelcomeModeHeight = 82, WelcomeCompactModeHeight = 72;
        public const float WelcomeHeadingHeight = 90, WelcomeCompactHeadingHeight = 74;
        public const float WelcomeEyebrowHeight = 24, WelcomeHintHeight = 28, WelcomeCreditHeight = 44;
        public const float WelcomeCompactHeight = 390, WelcomeLandscapeAspect = 1.2f;
        public const float WelcomeArtWidthFraction = .37f, WelcomeArtHeightFraction = .35f;
        public const float WelcomePortraitArtMaxHeight = 290, WelcomeMinArtHeight = 150;
        public const float WelcomeOutlineWidth = 1;
        public const int RoundedTextureSize = 32;
        public const float RoundedSpriteBorder = 10, SpritePixelsPerUnit = 100;
    }
}
