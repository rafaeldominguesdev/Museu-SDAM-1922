# Correções de controles e opções — 05/10/2026

Revisão dos fluxos de caminhada, opções, fichas, pausa e troca entre modo normal e VR.
As alterações anteriores do projeto foram preservadas.

APK compilado e assinatura v2 verificada: [Museu-Controles-VR-Corrigidos-2026-10-05.apk](Museu-Controles-VR-Corrigidos-2026-10-05.apk)
(133 MiB; Android 8+, ARM64; pacote `com.SemanaArteModerna.MuseudaSemanaArteModerna`).
Versão interna mantida em 1.1.0, código 2.

SHA-256: `ebb4002f43fc90a1ccf7b4f4350f6b05e5abdba60fa645572bbc8ab41a3d8825`.

| Problema encontrado | Correção |
| --- | --- |
| A preferência de inclinação transformava o controle de caminhada em um atalho para opções. | Caminhar continua solicitando movimento em ambos os modos. |
| A barra Caminhar/Menu permanecia sobre o cenário durante a visita. | Aparece ao baixar a cabeça e some ao olhar à frente; a direção fica estável durante a seleção. |
| Entrar já inclinado podia reutilizar a direção anterior da barra. | A direção é inicializada com a orientação atual da câmera. |
| Solicitar novamente um menu já aberto reposicionava o painel e reiniciava a seleção. | Abrir opções é idempotente e não funciona durante pausa. |
| O mesmo Voltar/Esc era processado por mais de um componente, fechando opções e saindo da visita. | Um único responsável fecha primeiro as opções; no VR, fechar opções mantém a pessoa no visor. |
| Botões ocultos podiam continuar selecionados após fechar opções ou entrar em VR. | A seleção é limpa nessas transições, assim como o destaque da mira ao reiniciar a interação. |
| O tempo parcial de olhar para uma obra sobrevivia à pausa. | A contagem é reiniciada durante a pausa; retomar exige uma nova permanência. |

## Uso no VR

1. Baixe a cabeça para mostrar Caminhar e Menu.
2. Mova a cabeça para colocar a mira sobre Caminhar por um segundo.
3. Levante a cabeça para iniciar o movimento. Baixe para parar.
4. Para abrir opções, mire em Menu. Olhar o céu não abre opções.

A caminhada por inclinação continua opcional nas configurações. No modo normal,
o botão de toque continua disponível, com segurar ou tocar conforme a preferência.

## Validação

| Execução | Resultado |
| --- | --- |
| Referência anterior às alterações | 213 verificações VR passaram; não cobriam o botão com inclinação salva. |
| VR, incluindo novos cenários | 226 verificações passaram. |
| Menu, toque e troca de modo | 148 verificações passaram; sete tamanhos de tela. |
| Movimento, colisão, pausa e retomada | 43 verificações passaram. |
| Fichas | 28 fichas, 12 cenários de estado, detecção das 28 peças e integração passaram. |

Os novos testes incluem deslocamento real do CharacterController durante vários
frames, não apenas a variável de velocidade. O teste de pausa também aguarda o
fade completo antes de exigir que a ficha esteja totalmente visível.

Logs: `validacao-vr.log`, `validacao-menu.log`, `validacao-movimento.log`,
`validacao-fichas.log` e `baseline-vr.log`. Capturas em `vr/`, `menu/` e `fichas/`.
Compilação registrada em `build.log`.

Limite: as poses VR foram simuladas no Editor. Não houve validação em celular
com visor físico; giroscópio, lentes e conforto precisam desse teste. A foto
original, isoladamente, não permite confirmar a causa da falha naquele aparelho.
