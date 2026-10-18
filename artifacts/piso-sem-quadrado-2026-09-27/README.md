# Retirada do reflexo quadrado do piso — 27/09/2026

APK: `Museu-Piso-Sem-Quadrado-27-09.apk`.

Compilação Android ARM64 concluída, 90 MiB. ZIP e assinatura v2 verificados; cópia idêntica colocada na Mesa.

SHA-256: `0c15a374814e3a31e9517d2c17f3a435a0ff0d4882522f7075e08c4fb099ea3b`.

## Causa reproduzida

O mapa de reflexo `Reflexo_da_galeria.asset` tinha uma face superior clara e faces laterais mais escuras, sem mipmaps. Ao refletir no piso, o limite entre as faces desenhava uma área quadrada clara em torno da câmera. As capturas com a sombra do sol desativada ainda mostraram o quadrado; retirar a reflexão fez a borda desaparecer.

Evidência mais clara: `piso-2-reflexo.png` (antes), `piso-2-sem-reflexo.png` (isolamento da causa) e `piso-2-corrigido.png` (resultado). Também há duas posições no parquet, `piso-0-*` e `piso-1-*`.

## Correção

- A cena usa um novo mapa de reflexo uniforme e discreto, com mipmaps, sem diferenças entre as seis faces.
- O gerador `LouvreMuseumSetup` usa a mesma configuração para não recriar o defeito.
- O mapa antigo foi preservado para comparação. Materiais, luzes diretas e focos das obras permanecem; os reflexos ambientais dos demais materiais também ficam mais uniformes.
- Aplicação reaproveitável: `MuseumModerna.MuseumFloorReflectionRepair.Apply`.

## Verificação

- `diagnostic.log`: três posições comparadas com e sem reflexo, antes da alteração.
- `validation.log`: 59 verificações aprovadas, processo encerrado com código 0. Incluem todas as faces e níveis do novo mapa, acendimento/desligamento dos 24 focos e preservação das sombras de contato.
- Capturas inspecionadas no Unity/Metal, incluindo parquet e medalhão de pedra; sem a borda quadrada após a correção.
- `git diff --check -- '*.cs'` aprovado.
- Sem teste em aparelho físico nesta atualização; confirmar o resultado no Android.
