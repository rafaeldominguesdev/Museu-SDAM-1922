# Leitura e opções no visor VR

Correções de 22/10/2026, posteriores à reorganização aprovada do acervo.

## Comportamento

- Opções gerais disponíveis somente a mais de 4 metros dos colisores de obras e fichas, incluindo placas de leitura. A distância é verificada em todas as direções, mesmo olhando para longe da obra. O centro do hall e pontos livres das salas permitem abrir o menu.
- A ficha abre centralizada na direção do olhar, voltada ao visitante. Pode sobrepor a obra para caber no campo de visão.
- A posição da ficha permanece fixa enquanto está aberta. Aproximar-se reduz a distância de leitura; girar a cabeça permite alcançar os controles.
- A ficha oferece apenas **Anterior** e **Próxima página**. Desviar o olhar fecha a leitura após a tolerância configurada. Os ajustes ficam no menu geral das áreas livres.
- A área liberada pelos controles recebe texto. Seções consecutivas compartilham páginas, preservando o conteúdo curatorial e suas fontes.

## Validação

Resultados da execução de 22/10, conferidos na retomada de 23/10/2026:

| Validação | Resultado |
| --- | --- |
| VrValidation.Run | 413 verificações de interação e recuperação VR aprovadas |
| Ativação pelo olhar | Seis cenários de dwell aprovados |
| AndroidMenuValidation.Run | 148 verificações de menu aprovadas; sete capturas |

Os testes incluem a restrição do menu por proximidade, centralização da ficha, aproximação sem deslocar o painel ou perder a página, controles pelo olhar e paginação das 31 fichas nos três tamanhos de texto. A primeira página também é verificada para aproveitar espaço com a seção seguinte.

Logs locais: `vr-final.log` e `menu-normal.log`. As execuções ocorreram após as últimas alterações dos scripts; nesta retomada os testes existentes foram conferidos, sem nova execução. Código da entrega: `32dc77baf12555cac3904b39dcbc8965c79e9e29`.

Captura do Editor conferida na retomada: [ficha de leitura](leitura.png). Ela mostra o painel centralizado, texto e os dois controles de página.

As verificações no Editor simulam poses e interação. Conforto, campo de visão efetivo nas lentes, rastreamento e desempenho precisam ser avaliados no celular com visor.

## APK Android

[Museu-Leitura-VR.apk](Museu-Leitura-VR.apk), gerado em 23/10/2026 no Unity 2022.3.62f1: 156.629.008 bytes (149,37 MiB).

SHA-256: `dfdc61a6172370732cf7aad0588f19a858cb8aa79c96a5686b3b8d491f8471a1`.

- Build concluído com `BUILD OK` e encerramento do Unity com código zero; log local `build-2026-10-23.log`.
- CRC das 1.228 entradas ZIP aprovado com `zipfile.testzip()`.
- Alinhamento aprovado com `zipalign -c 4`.
- Assinatura Android v2 aprovada com `apksigner verify --verbose`.

O APK anterior de curadoria foi preservado. O novo arquivo fica disponível localmente e é excluído do versionamento pelas regras existentes.
