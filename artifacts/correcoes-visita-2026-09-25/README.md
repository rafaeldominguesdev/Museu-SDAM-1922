# Correções da visita

Atualização de áudio: `Museu-Sem-Zumbido.apk` remove o loop `AmbientPad_Loop.wav` da cena e mantém os seis sons de passos e as correções abaixo. APK compilado, integridade ZIP e assinatura verificadas; sem teste físico nesta atualização. O arquivo de áudio original foi preservado no projeto.

SHA-256 da versão sem zumbido: `03f07c1b61c58596cd1a8a462cc4e4a34a990ce29f8f75927054947f769b9501`.

APK: `Museu-Corpo-Luzes-Movimento.apk` (Android ARM64, mínimo Android 8/API 26).

| Item | Alteração e evidência |
|---|---|
| Corpo | Camisa contínua, bacia conectada, braços e mãos visíveis. Cabeça permanece somente na sombra para não atravessar a câmera. Capturas `body-looking-straight-down.png` e `body-first-person-feet.png`. |
| Sombra ao redor do visitante | Duas cascatas substituídas por uma única projeção de sombra, com alcance de 12 m. Elimina a fronteira entre cascatas. A mancha específica das fotos não foi reproduzida no renderizador Metal do Editor; confirmar o resultado no Android/OpenGLES3. |
| Luz nas obras | 24 focos direcionados para as superfícies. Seleção dos três mais próximos, com transição gradual (durante a transição podem coexistir os focos que apagam e os novos). Comparação real `obra.png` / `obra-sem-foco.png`, diferença média de 2,69/255 na imagem renderizada. |
| Modo normal | Velocidade de 2,5 para 1,25 m/s; suavização do olhar independente de FPS, confirmação do gesto em 0,12 s e aceleração/frenagem mais responsivas. Continua exigindo olhar neutro após entrada, pausa ou menu. |
| Estrela no piso | Removidos apenas os triângulos do antigo medalhão em camadas nas três malhas de materiais; substituição por malha contínua, com estrela e anéis lado a lado. A geometria anterior é recuperável pelo Git. |
| Precisão da câmera | Near plane salvo em 0,1 m. Corrigido `MobileVrMode.Initialize`, que guardava o valor antigo de 0,01 m antes de `AutoBootstrap` e o restaurava ao entrar no modo normal. |
| Duplicação | Uma cena, uma raiz `Museum_LouvreStyle` e uma câmera ativa em runtime. Os tapetes antigos estão inativos. Não foi encontrado museu carregado duas vezes. |

## Validação executada

- Corpo: 23 verificações aprovadas, incluindo pernas, contato com piso, passos, parada, teleporte e recursos.
- Movimento: 28 verificações aprovadas. Com sensor simulado e gesto já armado, início em 0,150 s a 20 fps, 0,167 s a 30 fps e 0,167 s a 60 fps; parada em até 0,5 s. Essas medidas não incluem a latência do sensor físico.
- Visita: 6 verificações aprovadas, incluindo precisão da câmera após troca de modo, geometria do medalhão, orçamento de focos e diferença renderizada com/sem luz.
- VR: 32 verificações de interação e recuperação aprovadas, além de 6 cenários de ativação por permanência do olhar.
- APK compilado com sucesso, integridade ZIP e assinatura verificadas. Build inclui somente `MuseumScene`.
- `git diff --check -- '*.cs'` aprovado. Assets serializados pelo Unity mantêm a formatação YAML padrão do editor.
- Nenhum dispositivo apareceu em `adb devices`; não houve teste em aparelho ou visor físico.

Logs: `body-validation.log`, `movement-validation.log`, `visit-validation.log`, `vr-validation.log`, `build.log`.

SHA-256 do APK: `24a015d50f698396d199214e3d4a63ca7643252eac3f0d56f96ce01c250ad17f`.

## Manutenção

Reaplicar na cena: `MuseumModerna.MuseumVisitRepair.Apply`. O gerador Louvre usa `BuildMedallion` e o ajuste das imagens instala os focos via `ConfigurePictureLights`. O comando preserva as demais peças da arquitetura.

Validar no Unity com `-batchmode -projectPath ... -executeMethod ... -logFile ...`, sem `-quit` para testes em Play Mode:

- `MuseumModerna.BodyValidation.Run`
- `MuseumModerna.MovementValidation.Run`
- `MuseumModerna.MuseumVisitRepair.Capture`
- `MuseumModerna.VrValidation.Run`

Definir `MUSEUM_CAPTURE_OUTPUT` para direcionar as capturas. Para compilar, usar `BuildScript.BuildAndroid` com `MUSEUM_APK_OUTPUT` e `-quit`.
