# Museu noturno — melhorias visuais

## O que mudou

| Área | Alteração |
|---|---|
| Lua e estrelas | Céu direcional compartilhado por sete claraboias; estrelas sem alongamento por UV, halo discreto e lua com relevo procedural autoral. Sem cintilação animada. |
| Iluminação | Preenchimento mais neutro nas salas, luz lunar fria e lustre quente com sombras menos intensas. Preservado o limite de três focos de obras por proximidade. |
| Piso | Veios finos com mipmaps, sete tons próximos, variação de orientação e deslocamento entre tábuas, acabamento acetinado. |
| Molduras | Perfil externo mais estreito, mantendo a abertura original; ouro menos polido. |
| Paredes e teto | Grão mineral sutil em coordenadas de mundo, sem textura esticada em paredes grandes. |
| Vidros | Claraboias mais transparentes; vitrines sem sombras opacas sobre os livros. |
| Composição | Manifestos centralizados e parede curatorial própria na sala de Tarsila. |
| Plantas e cordões | Vaso afastado do retrato de Mário, variação de porte, sombras de contato nos vasos de Di e postes entre as obras, não diante das legendas. |
| Retratos | Recortes não destrutivos no enquadramento de Anita, Tarsila e Oswald; fotografias completas preservadas. |
| Literatura | Três vitrines com livros abertos cenográficos, explicitamente identificados como não históricos. Não foram inventadas capas ou fac-símiles. |

## Fonte do novo retrato de Oswald

- Acervo Arquivo Nacional, Fundo Correio da Manhã, código BR_RJANRIO_PH_0_FOT_08763_006; fotógrafo e data não identificados.
- [Histórico do arquivo no Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Oswald_de_Andrade,_sem_data%E2%80%93AI_enhanced.tiff#filehistory).
- Usada a versão original de **17/05/2018**, 2685 × 1929 pixels, não a versão retocada de 2020. A URL conserva o nome atual do arquivo, mas aponta para a revisão arquivada original.
- [Arquivo original arquivado](https://upload.wikimedia.org/wikipedia/commons/archive/3/3d/20201206223023%21Oswald_de_Andrade%2C_sem_data%E2%80%93AI_enhanced.tiff).
- Crédito: domínio público / Acervo Arquivo Nacional. A imagem anterior de 277 × 474 pixels continua no projeto, sem exclusão.

## Reprodução e limites

- Unity 2022.3.62f1. Reaplicar a cena: `MuseumModerna.MuseumThematicSetup.Apply`.
- Validar a cena salva: `MuseumModerna.ThematicValidation.Run`, com `MUSEUM_CAPTURE_OUTPUT` apontando para uma pasta de capturas. Não usar `-nographics` ao renderizar capturas.
- Céu, materiais e relevos gerados no Editor; sem geração procedural por frame. Texturas e materiais compartilhados entre salas.
- A lua é uma interpretação visual, não um mapa astronômico, e sua escala foi ampliada para leitura nas claraboias.
- Capturas e simulações no Editor não comprovam desempenho, sensores ou conforto em um celular/óculos físico. Essa verificação continua necessária no aparelho de destino.

## Verificação

| Teste | Resultado |
|---|---|
| Cena salva, percurso, fichas, iluminação e novos elementos | 271 verificações aprovadas |
| Interação e recuperação VR | 39 verificações aprovadas no Editor |
| Movimento | 38 verificações aprovadas com poses simuladas |
| Revisão visual | Capturas das salas, retratos, vitrines, molduras e claraboias inspecionadas |

As capturas desta pasta foram produzidas após reabrir a cena persistida. Isso evita usar buffers gráficos antigos do Editor após a regeneração das malhas.

| Entrega Android | Resultado |
|---|---|
| Compilação | Concluída com sucesso; APK de 140.072.585 bytes |
| Integridade ZIP | Sem erros |
| Alinhamento | `zipalign -c 4` aprovado |
| Assinatura | APK Signature Scheme v2 validado |
| Cópia | Mesa, sem substituir o APK anterior |

Arquivo: `/Users/rafaeldomingues/Desktop/Museu-Lua-e-Ceu-Estrelado-2026-10-05.apk`.

SHA-256 do APK: `9287024e8e78ee49e6e61dfc6f1c5b7b0d8beeca6e1876bfa4bc4749d9fd1f63`.

348 verificações automatizadas aprovadas no total (271 + 39 + 38). Não houve instalação nem medição de FPS em aparelho físico nesta entrega.
