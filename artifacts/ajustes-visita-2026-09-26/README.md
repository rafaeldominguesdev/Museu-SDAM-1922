# Ajustes da visita — 26–27/09/2026

APK: `Museu-Visita-Ajustada-27-09.apk`.

Build Android ARM64/API mínima 26, 90 MiB. Compilação encerrada com código 0; integridade ZIP e assinatura APK v2 verificadas. Uma cópia foi colocada na Mesa.

SHA-256: `5df38b07fa8f79dbc5fa5c261b7262f2c08d7ef6c734968e2d00d731895a8588`.

| Relato | Ajuste |
|---|---|
| Demora para voltar a andar após olhar uma obra | A barreira preserva a intenção de caminhar. A ficha VR também passa a apenas suspender a velocidade: ao sair dela, a caminhada retoma no primeiro frame. Menus, calibração e pausas continuam exigindo olhar neutro por segurança. Não foi encontrado um temporizador de 20 segundos. |
| Subir nas plantas | Os oito vasos receberam colisores com faces verticais de 1,8 m, substituindo cápsulas arredondadas de 68 cm. |
| Cordões faltando | 15 conjuntos protegem as 24 obras de parede, agrupados por parede e separados nos vãos maiores. Postes usam malha compartilhada otimizada. |
| Foco aparentemente sempre aceso | Alcance de ativação reduzido de 10 para 4,5 m. Mantidos três focos selecionados e transição suave. Testados acendimento e desligamento dos 24 focos; nenhuma luz de spot legada está ativa fora desse controle. O ponto exato do vídeo ainda não foi identificado. |
| Tronco e braços congelados | Balanço alternado dos braços em oposição às pernas, rotação discreta do tronco e retorno ao repouso. A animação usa o deslocamento real e não move a câmera. Implementação procedural própria, sem importar outro personagem. |
| Brilhos soltos no alto das paredes | Retiradas as três faixas decorativas parciais de cornija. Iluminação real do museu e focos das obras preservados. |

O áudio dos passos e a política que desativa a sombra dinâmica do sol no Android foram preservados.

## Validação final

| Verificação | Resultado | Log |
|---|---|---|
| Movimento | 34 verificações; retomada no primeiro frame após sair da ficha, colisão com obra, 20/30/60 fps e menus | `movement-validation.log` |
| Corpo e passos | 26 verificações; braços em oposição às pernas, tronco animado, câmera estável, repouso e passos sincronizados | `body-validation.log` |
| Focos e visita | 57 verificações; 24 focos acendem perto e apagam longe, nenhum spot legado ativo | `lights-verified.log` |
| Física geral | 76 verificações; quatro portais livres, chão, pilastras e mobiliário | `physics-verified.log` |
| Ambientação | 24 obras protegidas, 15 conjuntos testados com CharacterController, 32 tentativas de escalar vasos e seis percursos livres | `ambient-final.log`, `validacao.txt` |

Todos os processos finais encerraram com código 0. `git diff --check -- '*.cs'` aprovado. Nenhum dispositivo listado por `adb devices`: sensor físico, conforto e desempenho no celular ainda precisam de teste.

Os logs anteriores foram mantidos. A expansão dos cordões inicialmente excedeu o orçamento geométrico; postes foram simplificados de 24 para 12 lados, reduzindo o conjunto de 86.932 para 61.396 triângulos. O limite da validação foi atualizado para 65 mil, considerando a ampliação de seis para 24 obras. Os testes de luz foram corrigidos para simular o tempo explicitamente e distinguir os 24 focos gerenciados dos componentes legados já desativados. O teste das pilastras passou a começar pelo salão, evitando nascer dentro da parede, e mede a separação ao longo do trajeto para distinguir contornar de atravessar uma coluna redonda.

## Evidências visuais

- `caminhada.mp4`: três segundos da caminhada renderizada no Editor, com movimento de braços e pernas.
- `body-stride-30.png` e `body-stride-45.png`: poses durante a passada.
- `cordoes.png`, `entrada-e-plantas.png`, `plantas.png`: proteção e vegetação.
- `obra.png` / `obra-sem-foco.png`: comparação da iluminação real.

O Editor usa Metal e mantém sombras dinâmicas para diagnóstico do corpo; o Android mantém essas sombras desativadas, conforme a correção anterior.

## Pendências de conteúdo

O vídeo citado não foi anexado. É necessário para comparar a obra específica que parece permanecer iluminada e confirmar a região superior das paredes mencionada. A inclusão de novas obras depende de identificar a lista e as imagens pretendidas; o catálogo existente foi preservado.
