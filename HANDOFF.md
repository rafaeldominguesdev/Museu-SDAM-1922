# Handoff — Museu da Semana de Arte Moderna

Atualizado: 30/09/2026 — percurso com Hall e cinco salas.

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
