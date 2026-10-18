# Correções do museu — 22/09/2026

## APK

`Museu-VR-corrigido-22-09-2026.apk` — 94.851.874 bytes. Build Android IL2CPP ARM64 concluído, integridade ZIP e assinatura APK v2 verificadas. Mantida a versão interna 1.1.0 (código 2), pacote `com.SemanaArteModerna.MuseudaSemanaArteModerna`, Android mínimo 26 e alvo 35.

SHA-256: `72bf53b813933ba4cfe14f2863c664e519451da2d1329a2c631809056d3f64a9`.

Log: `android-build.log`. O novo APK tem nome próprio; não substitui os APKs anteriores.

## Resultado

- Física: gravidade e movimento resolvidos em um único `CharacterController.Move`; a pausa de caminhada não mantém o visitante suspenso. Pilastras e as duas faces dos quatro portais têm colisão, sem volumes duplicados.
- Altura: removido o metro extra do spawn. Na cena testada, pés em 0,020 m, olhos em 1,620 m e cápsula com 1,750 m. O piso é encontrado por colisão; móveis, teto e limites globais dos objetos não definem a altura do visitante.
- Personagem: duas pernas articuladas, joelhos e sapatos; passada acompanha o deslocamento real. Materiais foscos e quadril levemente atrás da câmera deixam as pernas visíveis. Camisa/cabeça/braços completam a sombra sem cobrir a visão.
- Iluminação: reaproveita uma única luz direcional, com ambiente consistente e sombra suave de 2048 px limitada a 8 m. O corpo não depende das sondas antigas de iluminação da cena.
- Home: recuperação do EventSystem e do Canvas ao voltar, estados de disponibilidade dos modos, proteção contra ações de botões já ocultos. “Voltar ao menu” também está nas opções da visita normal.
- VR: saída na primeira página; suspensão e recuperação de foco coordenadas; caminhada só retorna depois de uma pose utilizável; perda de rastreamento volta à preparação. Desativar o TrackedPoseDriver preserva posição e direção da câmera.

## Validação executada

Unity 2022.3.62f1; cena salva `Assets/Scenes/MuseumScene.unity`. Validação direcionada aos bugs desta entrega.

| Teste | Resultado | Evidência |
|---|---|---|
| AndroidMenuValidation | 129 verificações: toque via raycast/EventSystem, transições e sete tamanhos de tela | `menu-validation.log`, `menu-*.png` |
| PhysicsValidation | 76 verificações: colisão real do visitante, bancos, vitrine, pedestais, paredes, 20 colunas e quatro passagens | `physics-validation.log` |
| MovementValidation | 22 verificações: gesto, pausa, foco, colisão, gravidade e limite VR de 1,1 m/s | `movement-validation.log` |
| VrValidation | 32 verificações de interação e recuperação, mais seis casos unitários de permanência do olhar | `vr-validation.log` |
| BodyValidation | 18 verificações: medidas, proporção, passada, sombra e descarte de recursos | `body-validation.log` |
| Revisão dos arquivos | `git diff --check` passou nos scripts e testes alterados | Verificação local |

Capturas inspecionadas: `body-first-person-feet.png`, `body-external-shadow.png` e telas da home. `body-full-diagnostic.png` torna a parte superior visível apenas para inspeção; em produção ela contribui como sombra na primeira pessoa. As capturas do corpo são mono no Editor, 1440×1000, FOV de 62°; não medem desempenho Android nem óptica do visor.

## Uso no visor

1. Entre em Modo VR e prepare o visor.
2. Olhe à frente brevemente; abaixe a cabeça para caminhar e levante para parar.
3. Olhe para cima por cerca de 1,2 s para abrir opções. “Sair do VR” aparece na primeira página e é acionado mantendo o olhar no botão.

## Limite da validação

Nenhum Android conectado foi encontrado com `adb devices -l`. Rastreamento, conforto, lentes e desempenho precisam de conferência no celular/VR Box físico. Os cenários de movimento e recuperação usam poses simuladas no Editor. Não foi feita instalação em aparelho, publicação ou alteração de serviço externo.
