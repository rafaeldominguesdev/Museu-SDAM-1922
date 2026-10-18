# Visita completa — 02/10/2026

## Alterações

| Pedido | Implementação |
|---|---|
| Luz das obras por proximidade | Até três focos selecionados a 3,8 m no plano do piso; transição gradual; verificação de face e parede entre visitante e obra |
| Luz e sombra | Lustre com luz quente e sombra real; luzes locais nas salas; malhas arquitetônicas projetam sombras; removida a substituição automática pelo ciclo diurno legado |
| Velocidade | Lenta, normal e rápida em Opções da visita e na página 4 das Opções do visor; escolha salva entre sessões |
| Entrada VR | Escolha Modo VR prepara XR e entra automaticamente; sem botão, texto ou chamada de leitura de QR; falha de tracking ainda oferece retorno ao menu |
| Sala vazia | Foyer convertido em Seleção de obras com quatro reproduções do acervo existente: O homem amarelo, O farol, Meninas cariocas e Pescadores |
| Spawn | Centro do Hall, apoiado no piso, voltado para Manifestos |
| Sinalização | Placas maiores, alto contraste, acima dos acessos; retorno ao Hall em cada sala |
| Painel escuro | Mapa do percurso retirado por solicitação expressa; todas as pinturas renderizaram com imagem no Editor |

As quatro reproduções do foyer são uma seleção complementar das obras já presentes nas salas dos artistas, com cópia das fichas e fontes existentes, IDs próprios e proporções preservadas. Total: 30 fichas e 12 reproduções de oito pinturas diferentes.

### Velocidades

| Opção | Normal | VR |
|---|---|---|
| Lenta | 0,80 m/s | 0,60 m/s |
| Normal | 1,25 m/s | 0,90 m/s |
| Rápida | 1,80 m/s | 1,10 m/s |

## Validação

- ThematicValidation.ApplyAndRun: 260 verificações aprovadas, incluindo spawn, trajetos, 30 fichas, focos próximos e apagados pelo verso, lustre e retirada do mapa.
- VrValidation.Run: 39 verificações aprovadas, incluindo alteração de velocidade pelo olhar, compartilhamento com modo normal e ausência de QR na interface.
- MovementValidation.Run: 38 verificações aprovadas, incluindo caminhada nas três velocidades, parada, pausa, retomada e colisões.
- Capturas reais do Unity Editor: Hall, cinco salas, foyer, lustre, painel removido e cada reprodução. `mapa-antes-da-remocao.png` é evidência intermediária anterior à retirada, não a cena final.
- Código C#: `git diff --check -- '*.cs'` sem erros. Whitespace em YAML gerado pelo Unity não foi reformatado.
- Logs locais: thematic.log, vr.log, movement.log e build.log.

## Limitações

- Editor em Metal e simulação de VR; FPS, sombras em OpenGLES3, sensores e lentes ainda precisam de conferência no celular.
- O perfil de lentes previamente salvo continua sendo usado pelo SDK. Não há mais solicitação de leitura de QR no app.
- Cena anterior e APKs anteriores preservados. Repositório já continha alterações e arquivos não rastreados; nenhum commit, reset ou limpeza foi feito.
