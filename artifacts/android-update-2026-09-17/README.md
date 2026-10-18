# Atualização Android — 17/09/2026

## Entrega

APK final (94.842.022 bytes, 90,45 MiB): `Museu-VR-1.1.0-17-09-2026.apk`. Uma cópia idêntica está em `/Users/rafaeldomingues/Desktop/Museu-VR-1.1.0-17-09-2026.apk`.

- Novo ícone original, com arco, folha e cores modernistas. Arte: `../../UnityProject/Assets/Branding/MuseumModernIcon.png`.
- Abertura com duas opções: **Modo normal** e **Modo VR**. Layout ocupa o Canvas e respeita a área segura; a obra da abertura mantém sua proporção.
- Preparação VR só começa após a escolha. O QR ajusta lentes; na ausência de perfil salvo o Cardboard tem seu perfil padrão V1. Esse padrão não representa calibração específica do VR Box do visitante.
- Entrada e saída coordenadas, sem menu 2D por cima do VR. A visita VR entra diretamente; olhar para cima abre as opções.
- Velocidade e gesto de caminhada zerados ao trocar de modo, pausar, perder foco ou abrir menu. É necessário olhar à frente por 0,35 s e depois inclinar por 0,18 s; levantar o olhar em VR para imediatamente. O rumo não muda com roll perto da vertical.
- Entrada nativa aguarda a pose `CenterEyeRotation` fornecida pelo Cardboard, com timeout recuperável.
- Perfil de lentes é recarregado na entrada mesmo quando `HasDeviceParams()` já consumiu o contador de mudanças. Tela cheia e parâmetros de orientação/área útil são restaurados e atualizados.

## Verificações

| Verificação | Resultado |
|---|---|
| MovementValidation | PASS — 20 verificações de gesto, pausa, foco, colisão, altura, velocidade e menu |
| AndroidMenuValidation | PASS — 56 verificações, dois modos, transições, alvos de toque e texto sem cortes |
| Capturas de menu | 844×390, 1280×720, 2400×1080, 390×844; inspeção visual realizada |
| VrValidation | PASS — 6 cenários de dwell e integração de ficha/menu pelo olhar |
| Cena e catálogos | Preservação verificada por SHA-256 em `preserved-scene.sha256` |
| Aparelho e VR Box físicos | Não disponíveis nesta execução |
| APK Android | Integridade ZIP, assinatura v2, ARM64, pacote, minSDK 26, targetSDK 35 e ícones legacy/round/adaptive conferidos; versão 1.1.0 (código 2) |

Logs específicos estão nesta pasta. O teste antigo `ArtworkImageValidation` encontrou uma divergência pré-existente na cena atual: o nó `Reproducao_obra` de `homem_amarelo` esperado pelo validador não existe. A cena atual já estava assim antes destas alterações de interface/VR. Não regeneramos geometria nem substituímos a cena para forçar esse teste a passar. A falha ficou registrada em `preexisting-artwork-validation.log`; não é coberta pelos PASS acima. O arquivo Blender com obras e os arquivos das imagens foram preservados.

## Limite das lentes

Não existe máscara quadrada customizada no aplicativo. A silhueta e o campo visual são calculados pelo Cardboard a partir das lentes e do celular. Corrigimos atualização de perfil e aproveitamento de tela, mas **não afirmamos que as lentes ficaram maiores ou mais redondas**. Isso precisa de teste com o aparelho e o perfil do VR Box. Não alteramos distâncias físicas, separação ocular ou FOV por suposição.

Fontes técnicas: [API Cardboard](https://developers.google.com/cardboard/reference/unity/class/Google/XR/Cardboard/Api), [implementação oficial da distorção e fallback V1](https://github.com/googlevr/cardboard/blob/master/sdk/unity/xr_unity_plugin/cardboard_display_api.cc), [provider de entrada](https://github.com/googlevr/cardboard/blob/master/sdk/unity/xr_provider/input.cc), [ícones Android Unity](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/PlayerSettings.SetPlatformIcons.html).

## Ícone

Gerado com a ferramenta integrada `image_gen`; original copiado para o projeto. Prompt final: “Create one production Android launcher app icon for a Brazilian modern art virtual museum of Semana de Arte Moderna de 1922. Original abstract modernist artwork, no reproduction of any existing painting. A confident, sophisticated cut-paper / painted geometric composition suggesting a museum arch framing a stylized tropical leaf and a warm golden sun, highly simplified bold interlocking curved shapes, deep petrol green, warm ivory, ochre gold and restrained burnt terracotta. Subtle paper pigment texture, elegant museum identity, excellent legibility at tiny app icon sizes. Square 1:1 full bleed opaque image, no phone mockup, no outer rounded corners (Android applies shape), no border, no letters, no words, no numbers, no logo marks of existing brands. Keep the main recognizable arch-and-leaf motif inside the central 60% safe area so circular/adaptive icon cropping preserves it. Composition clean and striking, not a scene with many small objects. This is the actual new launcher icon asset, not a UI screenshot.”

## Preservação

O APK que estava na pasta de build permaneceu no local original e foi copiado para `Museu-anterior-preservado.apk` (SHA-256 `7da5b2fb1deccac4d4a61522a9e9ac3cecfb0b20c32155b8c456f3f4f488e434`). O APK de 16/09 que já estava no Desktop também não foi substituído. O novo build recebe nome próprio e o script cria backup automaticamente antes de eventual reutilização de caminho.
