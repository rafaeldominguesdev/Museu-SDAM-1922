# Interações — 05/10/2026

## APK

- Arquivo: `Museu-Interacoes-2026-10-05.apk`.
- Cópia idêntica na Mesa, sem substituir a versão anterior.
- Tamanho: 140.078.781 bytes.
- SHA-256: `65313474d86adad098e3c9c7202fd482c676199214a93c4fa273165b57d752ca`.
- Build Unity bem-sucedido; integridade ZIP, alinhamento de quatro bytes e assinatura Android v2 verificados.

## Mudanças

- Céu livre: olhar para a lua e as estrelas não abre mais as opções VR.
- VR: olhe por um segundo para **Caminhar** na barra abaixo; volte ao horizonte para andar. Baixe a cabeça para parar. **Menu** abre as opções.
- Normal: segure **Segure para caminhar**; solte ou retire o dedo da área para parar. Opções da visita pausam a caminhada.
- A caminhada por inclinação continua disponível como opção; trocar o modo cancela qualquer comando de movimento anterior.
- Fichas abrem após 0,55 s, com 1,5 s para mudar o olhar antes do fechamento (ou 5 s na leitura longa).
- VR: fichas ao lado da obra, botão Reposicionar, texto maior, seções e paginação por frases; menu reduzido a duas páginas.
- Mantidos o céu noturno, a lua com relevo, as sete claraboias, as vitrines e o acervo com 28 fichas da etapa anterior.

## Verificação

| Suíte | Resultado |
|---|---|
| VR | 213 verificações e seis cenários de dwell aprovados |
| Movimento | 42 verificações aprovadas |
| Menu | 134 verificações aprovadas; sete tamanhos de tela |
| Acervo e ambiente | 271 verificações aprovadas |

- Total: 660 verificações em execução no Editor, além de seis cenários isolados de dwell.

- As 28 fichas foram verificadas nos três tamanhos de texto, sem cortes de títulos ou páginas.
- Testes no Unity Editor, com poses simuladas. Não substituem validação de lentes, sensores, FPS e conforto no aparelho.
- Capturas em `vr/`, `menu/` e `ambiente/`.
- Esta é uma etapa de implementação da lista de melhorias, não a conclusão de todas as sugestões.
