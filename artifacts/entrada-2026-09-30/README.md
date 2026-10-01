# Passo 1 — entrada e centro do Hall

Implementado em 30/09/2026.

- A cena salva possui `Museum_LouvreStyle/Spawn_Entrada`, um Transform vazio no foyer, em `(0, 0, -13)`, voltado para o Hall.
- `PlayerSpawnAnchor` usa essa âncora, apoia os pés no piso físico e substitui a posição antiga restaurada pelo save. Sem âncora, usa a posição original do visitante na cena.
- O objeto `MuseumCeilingBuilder` continha um BoxCollider ativo de 1 × 1 × 1 m na origem, embora seu script estivesse desativado e não houvesse malha. Esse colisor foi desativado de forma reversível; os colisores do teto não foram alterados.
- O pedestal central legado já estava desativado e continua preservado assim.
- O gerador de arquitetura reaplica a configuração da entrada.

## Validação

Unity 2022.3.62f1, execução em Play Mode via `MuseumModerna.MuseumEntranceSetup.ApplyAndValidate`: **85 verificações aprovadas**, saída 0.

Cobertura: início real no foyer; âncora sem renderização ou colisão; posição antiga no centro substituída; centro livre de obstáculos; caminhada real do foyer ao Hall e através do centro; piso, paredes, colunas, móveis e portais. O primeiro teste detectou o colisor residual; a execução final passou após desativá-lo.

Log completo local, não versionado por incluir dados de máquina/licenciamento: `validation.log`.

Cena e scripts atualizados. APK ainda não recompilado nesta etapa; sem validação em aparelho físico.

## Próximo passo

Redistribuir a planta para Hall conectado a cinco salas: Mário de Andrade, Oswald de Andrade, Anita Malfatti, Di Cavalcanti e Manifestos. A etapa de arquitetura e a atualização do conteúdo ainda não foram executadas.

Atualização posterior, em 30/09/2026: essa etapa foi concluída; ver `../cinco-salas-2026-09-30/README.md`. O texto acima preserva o estado ao término do passo 1.
