# Handoff — Museu da Semana de Arte Moderna

Atualizado: 23/10/2026 — entrega do APK com opções VR por distância, leitura centralizada e paginação simplificada.

## Estado vigente — 23/10/2026

- Cena regenerada no Unity 2022.3.62f1: 31 fichas, 12 pinturas e cinco retratos documentais.
- Sala lateral de Oswald convertida em Sala dos Manifestos; os quatro livros e a biografia de Oswald estão na sala ao fundo.
- Tarsila reúne A Negra, A Cuca, Abaporu e Operários. Pescadores corresponde à pintura de 1951 do MAC-USP, com reprodução da Revista USP.
- VR caminha ao olhar para baixo e para ao levantar a cabeça. Velocidades: 0,8 / 1,25 / 1,6 m/s. O botão Menu é lateral, disponível somente a mais de 4 m dos expositores e oculto durante o gesto de caminhada.
- Fichas VR abrem até 3 m, após permanência do olhar, centralizadas e voltadas ao visitante. Ficam fixas no espaço para permitir aproximação. Apenas Anterior e Próxima página; texto ocupa a área liberada pelos controles, com seções compartilhando páginas.
- Editor: última etapa com 413 verificações de VR, seis cenários de dwell e 148 de menu normal aprovados em 22/10; logs conferidos em 23/10, sem nova execução. A etapa anterior registrou 360 verificações de acervo/circulação e 43 de movimento.
- APK atual: `artifacts/vr-leitura-2026-10-22/Museu-Leitura-VR.apk`, gerado em 23/10, 149,37 MiB; build, CRC ZIP, alinhamento e assinatura v2 aprovados. SHA-256 no relatório de leitura VR. APK anterior de curadoria preservado.
- Fontes, análise das obras e capturas: [curadoria de 21/10](artifacts/curadoria-2026-10-21/FONTES.md). Resultado da entrega: [relatório](artifacts/curadoria-2026-10-21/README.md).
- Aparelho físico: lentes, sensores, conforto, FPS e aquecimento continuam pendentes de validação.
- Correções posteriores ao acervo aprovado: [leitura e opções VR](artifacts/vr-leitura-2026-10-22/README.md).

## Histórico — 05/10/2026

| Item | Estado | Observação |
|---|---|---|
| Acervo e ambiente | Preservados da etapa visual de 05/10 | 28 fichas; obras nas salas dos autores; cinco retratos documentais; Tarsila com Abaporu |
| Lua e céu estrelado | Preservados | Shader direcional compartilhado por sete claraboias, lua com relevo, luz lunar e ambiente noturno |
| Visita normal | Implementado | Segurar botão para caminhar; soltar/sair da área para parar; opções pausam a visita |
| Visita VR | Implementado | Botões Caminhar/Menu abaixo do horizonte; olhar para cima não abre menu; olhar à frente após comando inicia marcha; baixar a cabeça para |
| Modo antigo | Disponível | Caminhada por inclinação opcional, persistida em `MuseumTiltWalking`; padrão novo por botão |
| Fichas | Implementado | Dwell mínimo 0,55 s; tolerância após sair do olhar 1,5 s ou 5 s; painel VR ao lado da obra; reposicionamento; seções e quebras por frases |
| Menu VR | Simplificado | Duas páginas, seis ações por página e alvos maiores; velocidades disponíveis na primeira página |
| Testes VR | Aprovados | 213 verificações de execução + seis cenários de dwell, incluindo as 28 fichas nos três tamanhos de texto |
| Testes de movimento | Aprovados | 42 verificações, incluindo comando manual, parada, pausa, foco, inclinação legada e colisões |
| Testes de menu | Aprovados | 134 verificações, sete resoluções, pressionar/soltar/sair do botão e pausa das opções |
| Testes temáticos | Aprovados | 271 verificações de acervo, circulação, retratos, luzes, claraboias, lua e vitrines |
| APK Android | Gerado e verificado | `artifacts/interacoes-2026-10-05/Museu-Interacoes-2026-10-05.apk`; 140.078.781 bytes; ZIP, alinhamento e assinatura v2 aprovados |
| Aparelho físico | Pendente | Lentes, conforto, sensores, FPS e aquecimento não validados no Editor |

### Execução e arquivos desta etapa

- Scripts: `HeadGazeMovement`, `GazeDwellInteraction`, `MuseumGuideTheme`, `MuseumGuidePanel`, `MuseumWalkButton` (novo), `VrWorldGuide`, `MobileVrMode`.
- Testes: `VrRuntimeValidation`, `MovementRuntimeValidation`, `AndroidMenuRuntimeValidation`.
- Evidências: `artifacts/interacoes-2026-10-05/vr/` e `artifacts/interacoes-2026-10-05/menu/`.
- Entrega visual anterior preservada: `artifacts/visual-noturno-2026-10-05/Museu-Lua-e-Ceu-Estrelado-2026-10-05.apk`.
- APK atual: `artifacts/interacoes-2026-10-05/Museu-Interacoes-2026-10-05.apk`; SHA-256 `65313474d86adad098e3c9c7202fd482c676199214a93c4fa273165b57d752ca`.
- Cópia na Mesa: `/Users/rafaeldomingues/Desktop/Museu-Interacoes-2026-10-05.apk`, com o mesmo SHA-256; versão anterior preservada.
- O histórico de 02/10 abaixo descreve versões anteriores: a contagem de 31 fichas, Abaporu nos manifestos e quatro páginas de menu VR já não são o estado atual.

### O que funcionou e o que foi corrigido

- Separar o consumo do olhar pela barra de controles do bloqueio de caminhada pela ficha evita cancelar o comando antes de o visitante olhar à frente.
- O teste VR por botão inicialmente falhou porque o Editor em batch estava sem foco; normalizada a notificação de foco no teste, preservando a proteção no aplicativo.
- A primeira paginação deixava o fim de uma frase isolado; agora prioriza limites de frases/parágrafos quando ocupam pelo menos metade da página.
- Skills de redesign/design-code sem os arquivos de apoio locais: aplicado o tema Unity existente; não foi executada nem alegada certificação WCAG/ARIA ou aprovação dos scripts ausentes.
- Nenhum reset, limpeza ou commit: alterações anteriores do usuário preservadas.

### Próximos passos vigentes

1. Validar esta versão em Android/visor físico, especialmente descoberta dos controles abaixo do horizonte e conforto ao parar.
2. Medir desempenho antes de ampliar sombras, resolução ou pós-processamento.
3. Continuar a lista de melhorias visuais e interativas; esta etapa não representa conclusão de todo o levantamento.

## Histórico — 02/10/2026

- APK: `artifacts/ambiente-noturno-2026-10-02/Museu-Noite-Estrelada-2026-10-02.apk` e cópia idêntica na Mesa.
- SHA-256: `15186b97b8735fff9cf33c2a6c73536527ac8ed72ba9c8ee29fcfaa848bd644b`.
- 31 fichas ativas: 27 do catálogo e quatro reproduções complementares no foyer.
- Abaporu original de 1928 incluído na sala de manifestos, com reprodução institucional do MALBA e contexto explícito de que é posterior à Semana.
- A ficha biográfica vazia de Di Cavalcanti virou retrato documental de 1964 do Arquivo Nacional, em domínio público.
- Seis cabeçalhos de sala redesenhados: placa escura, filetes dourados, número, título e subtítulo separados.
- Placas das obras ganharam fundo próprio e hierarquia de título/período/crédito; a fonte é pré-carregada para estabilizar o atlas e evitar glitches.
- Sala Di Cavalcanti possui quatro vasos, um em cada canto, sem bloquear porta, quadros ou cordões.
- Sete claraboias mostram céu estrelado e lua; seis salas e o Hall recebem luz lunar fria com sombras, preservando a luz quente do lustre.
- Spawn voltou ao centro do Hall. O mapa escuro do percurso foi retirado a pedido do usuário.
- Focos das obras acendem por proximidade (3,8 m, seleção dos três mais próximos, sem ativação pelo verso ou através de parede).
- Lustre ilumina de verdade; arquitetura projeta sombras; `LouvreGalleryLighting.ApplyGallery` mantém o ambiente e `AutoBootstrap` não adiciona o ciclo diurno na planta temática.
- Três velocidades persistidas em `MuseumWalkSpeed`, disponíveis em opções normais e na página 4 do menu VR.
- Entrada VR automática após preparo do XR, sem leitor QR. A recuperação de falha permanece manual para evitar um ciclo de reentrada sem tracking.
- Validações: temática 277, VR 39 e movimento 38, todas com saída 0 no Editor.
- APK de 112 MiB: ZIP íntegro, `zipalign -c 4` aprovado e assinatura Android v2 válida.
- Teste em celular/visor físico permanece pendente, em particular sombras OpenGLES3, FPS e conforto.
- Usada a skill handoff para registrar o resultado e preservar as decisões anteriores abaixo.

## O que foi executado nesta etapa

| Item | Estado | Resultado |
|---|---|---|
| Conteúdo | Feito | Abaporu correto e retrato de Di Cavalcanti adicionados com fonte e crédito |
| Sinalização | Feito | Cabeçalhos e placas de obras redesenhados; fonte pré-carregada |
| Ambientação | Feito | Quatro vasos nos cantos da sala Di Cavalcanti |
| Claraboias | Feito | Vidro corrigido de opaco para transparente; estrelas, lua e luz fria visíveis |
| Validações | Aprovado | 277 temática, 39 VR, 38 movimento |
| APK Android | Gerado | 112 MiB, assinatura v2, alinhamento e integridade aprovados |
| Aparelho físico | Pendente | Instalação, FPS, leitura e conforto em Android/visor |

## Decisões desta etapa

- O arquivo `texturas/abaporu.jpg` não foi usado: era uma releitura visualmente incorreta, não a obra de Tarsila.
- A reprodução correta veio da página oficial da coleção MALBA; o retrato veio do Arquivo Nacional via Wikimedia Commons.
- O céu é uma textura procedural local, evitando dependência de internet no aplicativo.
- A iluminação lunar usa spots de teto com sombras suaves; o lustre continua quente para manter contraste e legibilidade.

## Próximos passos vigentes

1. Instalar `Museu-Noite-Estrelada-2026-10-02.apk` em Android ARM64.
2. Conferir se céu, lua, sombras e 31 focos mantêm FPS aceitável no aparelho real.
3. Testar legibilidade dos novos cabeçalhos e fichas em visita normal e no visor VR.

## Estado anterior — 02/10/2026

- Entrega anterior: `artifacts/visita-completa-2026-10-02/README.md`.
- Essa versão tinha 30 fichas, antes da inclusão do Abaporu e do retrato documental.

## Histórico — entrega de 30/09/2026

Os registros abaixo descrevem a entrega anterior. Spawn no foyer, mapa e contagens de 26 fichas foram substituídos pelo estado vigente acima.

## Objetivo

- Entregar um Hall de introdução e contexto histórico ligado diretamente a quatro salas de artistas e uma de manifestos.
- Preservar o museu existente, os controles de visita normal/VR e as alterações anteriores do usuário.

## Progresso atual

| Item | Estado | Observação |
|---|---|---|
| Entrada e bloco central | Feito | Spawn invisível no foyer; colisor residual central desativado |
| Hall e cinco salas | Feito | Mário, Oswald, Anita, Di Cavalcanti e Manifestos |
| Conteúdo | Feito | 26 fichas: 3 do Hall, 4 biografias, 16 obras e 3 manifestos |
| Reproduções | Feito | Oito pinturas com imagem e crédito; livros com painéis didáticos |
| Navegação e conteúdo | Validado no Editor | 138 verificações aprovadas |
| Interação VR | Validado no Editor | 32 verificações aprovadas; sem lentes físicas |
| APK Android | Gerado e verificado | 108,15 MiB; assinatura v2, integridade ZIP e alinhamento aprovados |
| Aparelho físico | Pendente | Instalação, legibilidade, tracking, desempenho e conforto |

## O que foi executado nesta sessão

- `MuseumModerna.ThematicValidation.ApplyAndRun` → gera/salva a cena e executa 138 verificações em Play Mode; saída 0.
- `MuseumModerna.VrValidation.Run` → 32 verificações de olhar, paginação, fixação, saída, suspensão e recuperação; saída 0.
- `BuildScript.BuildAndroid` → APK ARM64 com build bem-sucedido em aproximadamente 42 segundos; saída 0. `apksigner verify`, `unzip -tq` e `zipalign -c` aprovados.
- Capturas renderizadas das cinco salas, Hall, mapa, detalhe de pintura e biografia conferidas visualmente.
- `UnityProject/Assets/Editor/LouvreMuseumSetup.cs`: método `BuildThematicArchitecture`, sem substituir os geradores antigos.
- `UnityProject/Assets/Editor/MuseumThematicSetup.cs`: instalador reaplicável, arquivo do ambiente anterior, criação de painéis, imagens, sinalização e fichas.
- `UnityProject/Assets/Resources/MuseumThematicCatalog.json`: fonte editável das 26 fichas, incluindo referências históricas e créditos.
- `UnityProject/Assets/MuseumThematic/`: dados e materiais gerados, separados dos assets antigos.
- `UnityProject/Assets/Scripts/Museum/PillarPhysicsRepair.cs`: não reaplica colisores da planta antiga quando encontra `Planta_CincoSalas`.
- `UnityProject/Assets/Editor/ArtworkTextureSetup.cs`: importação de textura reutilizável pelo novo instalador.
- `UnityProject/Assets/Tests/Editor/ThematicValidation.cs` e `Tests/ThematicRuntimeValidation.cs`: testes da nova planta e capturas.
- `UnityProject/Assets/Tests/VrRuntimeValidation.cs`: aceita o novo ID `anita_homem` sem perder compatibilidade com a planta anterior.
- Etapa anterior: `MuseumEntranceSetup.cs` e `PlayerSpawnAnchor.cs`, documentada em `artifacts/entrada-2026-09-30/README.md` (85 verificações da planta antiga).

## O que funcionou

- Cinco trajetos reais de ida e volta usando o CharacterController, com paredes divisórias bloqueando passagem indevida.
- Novos painéis preservam PaintingExhibit, seleção pelo olhar e fichas normais/VR.
- Extração direta de imagens incorporadas em catálogos; sem recriação por IA nem capas fictícias.
- Marcação explícita de obras/manifestos anteriores e posteriores a 1922.

## O que não funcionou

- Primeira migração arquivou a interface junto da decoração → corrigido preservando qualquer raiz que contenha UIManager, inclusive RectTransform.
- Molde de moldura tinha abertura de 1,40 × 1,10 m, menor que o novo painel → escala ajustada para 2,38 × 1,87 m.
- Importador removia a emissão por `EmissiveIsBlack` → configurado `MaterialGlobalIlluminationFlags.None`; testes verificam `_EMISSION` nos painéis e imagens.
- Bake de occlusion culling corresponde à planta anterior → desativado nas câmeras desta cena. Um novo bake fica como otimização futura.

## Decisões e trade-offs

- Arquitetura e exposição antigas ficam sob `Museum_PreviousLayout`, inativo. Não apagar esse arquivo sem revisar o trabalho preservado.
- Apenas `Museum_LouvreStyle` e `Museum_GuidedExhibition` gerados são substituídos ao reaplicar. Interface, Player e gestores continuam ativos.
- Tarsila/Abaporu contextualizados na Antropofagia, sem sexta sala: o pedido principal especificou quatro artistas.
- *Macunaíma* identificado como destaque da sala de Mário.
- *Pescadores*: versão de 1946, 33 × 46 cm, catálogo Almeida & Dale, p. 176. Confirmar com o grupo se outra versão era pretendida.
- O livro *Pau Brasil* (1925) é distinto do manifesto (1924).
- Novos arquivos de geometria usam prefixo `Architecture_Tematico_`; assets antigos preservados.

## Problemas em aberto / riscos

- Testes de VR usam simulação no Editor, não validam lentes, sensores, FPS, aquecimento ou conforto físico.
- Repositório já tinha muitas alterações e arquivos não rastreados antes desta etapa. Nenhum reset, limpeza ou commit realizado.
- Testes antigos de geometria/conteúdo esperam a planta anterior. Para esta planta, usar `ThematicValidation`; não interpretar suas contagens antigas como requisito atual.
- Reproduções públicas não equivalem a autorização comercial. Fontes, créditos e identificação dos arquivos estão em `artifacts/cinco-salas-2026-09-30/FONTES.md`.

## Próximos passos

1. Instalar `artifacts/cinco-salas-2026-09-30/Museu-Cinco-Salas.apk` num Android ARM64 e percorrer Hall → cada sala → Hall, em visita normal e VR.
2. Conferir legibilidade e conforto com o visor real; ajustar tamanhos/distâncias se necessário.
3. Validar com o grupo a versão de *Pescadores* e o escopo de Tarsila antes de ampliar salas.

## Reprodução técnica

- Unity: `2022.3.62f1`; projeto: `UnityProject`.
- Cena: `UnityProject/Assets/Scenes/MuseumScene.unity`.
- Instalar/reaplicar: menu `Museum/Percurso/Instalar Hall e cinco salas`.
- Via CLI: Unity `-batchmode -projectPath <UnityProject> -executeMethod MuseumModerna.ThematicValidation.ApplyAndRun -logFile <log>`; não usar `-quit`, pois o teste encerra o Editor.
- VR: mesmo comando com `-executeMethod MuseumModerna.VrValidation.Run`.
- Build: definir `MUSEUM_APK_OUTPUT` para o novo caminho; executar Unity com `-batchmode -quit -executeMethod BuildScript.BuildAndroid`.
