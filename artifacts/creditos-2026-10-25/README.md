# Autoria na abertura e retorno ao menu dentro do visor

Alterações de 25/10/2026.

## Comportamento

- A abertura apresenta os créditos de autoria abaixo dos dois botões de modo. O texto é definido em `WelcomeScreen.cs`, compartilhado com a tela inicial VR e usa a tipografia existente.
- O menu inicial dentro do visor aparece somente ao retornar da visita, pelas opções ou pelo comando Voltar do Android. O painel usa o Canvas worldspace, o shader e o acionamento pelo olhar já existentes no guia VR.
- O retorno mantém os subsistemas e o rastreamento ativos. A caminhada fica bloqueada enquanto o menu está aberto, inclusive olhando para fora do painel.
- **Continuar em VR** ou **Voltar** retoma a visita e exige um novo gesto de caminhada. **Modo normal** encerra o VR e retoma a visita 2D.
- Ao recuperar o foco do aplicativo, o menu inicial VR permanece aberto; falhas de tracking continuam encaminhando à preparação existente.

## Validação no Editor

| Validação | Resultado |
| --- | --- |
| AndroidMenuValidation.Run | 170 verificações aprovadas; sete resoluções |
| VrValidation.Run | 422 verificações de interação e recuperação aprovadas |
| Ativação pelo olhar | Seis cenários de dwell aprovados |
| Capturas | Abertura em paisagem e retrato e menu inicial VR conferidos |

As verificações incluem nomes completos, ausência de corte e sobreposição, área segura, dois modos de visita, retorno VR sem tela 2D sobreposta, controle pelo olhar, Voltar, recuperação de foco e saída para o modo normal. O título do menu VR também é verificado pelo número de caracteres visíveis gerados.

Evidências resumidas: [abertura](abertura.png) e [menu no visor](menu-vr.png). Os logs locais são `menu-final.log` e `vr-final.log`; as capturas completas ficam em `menu/` e `vr/`.

As capturas do Editor são monoscópicas. Não validam renderização efetiva nas lentes, sensores, conforto, FPS ou aquecimento no celular com visor.

## APK Android

[Museu-Creditos-VR.apk](Museu-Creditos-VR.apk), gerado em 25/10/2026 no Unity 2022.3.62f1, a partir do código `83a1402db91fc3865530c5ae0ceeb322b6d24d91`: 156.630.936 bytes (149,37 MiB).

SHA-256: `029160c4a924de875c424647d927fed85ec7918f815dd8f5739d1707c83369d9`.

Build concluído com `BUILD OK` e encerramento do Unity com código zero (`build.log`). CRC das 1.228 entradas ZIP, `zipalign -c 4` e assinatura Android v2 (`apksigner verify --verbose`) aprovados. O APK é um arquivo local excluído do Git pelas regras existentes.
