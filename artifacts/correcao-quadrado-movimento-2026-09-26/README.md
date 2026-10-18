# Movimento e iluminação — 26/09/2026

APK: `Museu-Movimento-Luz-26-09.apk`.

Build concluído: 90 MiB, Android ARM64/API mínima 26. Integridade ZIP e assinatura APK v2 verificadas.

SHA-256: `39d19912631f1300f6cde35257909990dde9bfb4b5fdec69758674939a2f7fde`.

| Relato | Alteração | Evidência e limite |
|---|---|---|
| Personagem para de andar | A barreira perto das obras zera apenas a velocidade, preservando o gesto. Virar para um caminho livre permite continuar sem levantar a cabeça. | Regressão reproduzida antes da correção; teste de aproximação, espera e saída aprovado depois. |
| Quadrado claro ao redor do visitante | No Android, desativada a projeção de sombra da luz direcional. A política vale tanto no modo normal quanto no VR. | Contorno para a provável origem do recorte. O efeito original não foi reproduzido no Editor/Metal; precisa de confirmação no celular. A sombra dinâmica do personagem também deixa de aparecer no Android. |

A luz do sol, o ciclo de iluminação, os focos dos quadros e as sombras de contato dos móveis permanecem ativos. O Editor mantém a sombra dinâmica para diagnóstico; as capturas desta pasta exercitam explicitamente a política do Android.

## Validação

- `movement-before.log`: falha reproduzida ao verificar que a barreira da obra preserva a intenção de caminhada.
- `movement-validation.log`: 31 verificações aprovadas, incluindo início e parada a 20/30/60 fps, colisões, saída da obra, pausas e menu VR. Encerramento com código 0.
- `visit-validation-retry.log`: 7 verificações aprovadas; política de sombra persiste após ciclo e troca de modo, sombras de contato permanecem ativas e focos alteram a imagem renderizada em média 2,69/255. Encerramento com código 0.
- A primeira captura (`visit-validation.log`) completou as verificações, mas o processo terminou com código 139. O teste passou a aguardar dois frames para liberar os recursos antes de sair; a repetição encerrou normalmente.
- Capturas reais do Unity: `galeria.png`, `estrela.png`, `obra.png` e `obra-sem-foco.png`.
- Nenhum aparelho listado por `adb devices`; sem validação de giroscópio físico ou renderização OpenGLES3 no celular.

## Conferência no celular

1. Instalar o APK desta pasta e entrar na visita.
2. Caminhar olhando para baixo até perto de uma obra. Virar para se afastar sem levantar a cabeça: deve retomar a caminhada.
3. Girar e caminhar observando o piso ao redor do personagem: verificar se o quadrado desapareceu.
4. Abrir e fechar o menu: nessa situação, levantar o olhar e baixar novamente continua sendo necessário para retomar.

Se o quadrado persistir, registrar um vídeo, o modelo do celular e o modo usado (normal ou VR), pois isso indica uma origem diferente do passe de sombra retirado.
