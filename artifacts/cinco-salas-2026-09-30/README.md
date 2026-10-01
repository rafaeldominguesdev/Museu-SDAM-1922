# Hall e cinco salas — entrega de 30/09/2026

## Implementado

- Hall central: sociedade da época, introdução ao Modernismo e Semana de 1922.
- Salas de Mário de Andrade, Oswald de Andrade, Anita Malfatti e Di Cavalcanti: uma biografia e quatro obras em cada, com relação com a Semana.
- Sala de manifestos: Pau-Brasil (1924), Antropófago (1928) e Verde-Amarelo (1929), com resumos introdutórios.
- 26 fichas interativas, oito reproduções de pinturas e sinalização do percurso.
- Entrada no foyer, centro livre e cinco acessos diretos ao Hall; nenhuma sexta sala adicionada.
- Visita normal e controles VR por olhar preservados.
- Ambiente anterior arquivado de forma reversível sob `Museum_PreviousLayout`, inativo na cena.

## Validação

| Verificação | Resultado |
|---|---|
| Planta, colisões, conteúdo, olhar e materiais | 138 verificações aprovadas em Play Mode |
| Interação/recuperação VR | 32 verificações aprovadas em simulação no Editor |
| Máquina de ativação por permanência do olhar | 6 cenários adicionais aprovados |
| Capturas renderizadas | Cinco salas, Hall, mapa, pintura, biografia e guia VR revisados |
| Compilação Android | Sucesso; saída 0 |
| Integridade ZIP | Sem erros |
| Assinatura APK | Válida, esquema v2, um assinante |
| Alinhamento | `zipalign -c -v 4` aprovado |
| Aparelho/visor físico | Não testado |

Logs locais (não versionados, pois incluem dados de máquina/licenciamento): `validation.log`, `vr-validation.log`, `build.log`. Os resultados estão resumidos acima.

## APK

- Arquivo: `Museu-Cinco-Salas.apk`.
- Tamanho: 113.405.136 bytes (108,15 MiB).
- Pacote: `com.SemanaArteModerna.MuseudaSemanaArteModerna`.
- Versão: 1.1.0, código 2 — mantidos os valores do projeto; o nome do arquivo identifica esta entrega.
- Android mínimo: 8.0 / API 26; target API 35; ARM64; OpenGLES 3.
- SHA-256: `9db495c24dd02aba4bd2e94fc0c0038b1ae6d8d019060a202373619ceb19e2c1`.
- APKs anteriores não foram substituídos.

## Observações de conteúdo

- *Macunaíma* está identificado como destaque da sala de Mário.
- O livro *Pau Brasil* (1925) não é o manifesto de 1924.
- Obras e manifestos posteriores a 1922 são explicitamente identificados; não se apresenta toda a seleção como exposta na Semana.
- *Pescadores*: selecionada a versão de 1946, 33 × 46 cm. O título é usado em diferentes obras; confirmar com a equipe se pretendia outra reprodução.
- Tarsila e *Abaporu* aparecem no contexto da Antropofagia, mantendo as quatro salas de artistas especificadas.
- Fontes e créditos: `FONTES.md`. As imagens foram conferidas e extraídas de reproduções documentadas, não geradas por IA.

## Próxima verificação

Instalar o APK no celular e testar a circulação pelas cinco salas, abertura/paginação das fichas, retorno ao Hall, entrada/saída de VR, suspensão e retomada. Conferir leitura, tracking, FPS e conforto com o visor real. Não interpretar os testes do Editor como validação de hardware.

Continuidade técnica e decisões: `../../HANDOFF.md`.
