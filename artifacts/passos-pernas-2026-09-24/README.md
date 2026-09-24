# Passos e pernas

- A animação controla os contatos dos pés; o áudio usa esses mesmos eventos.
- Apoio linear reduz o deslizamento; a perna em balanço tem retorno suave, flexão do joelho e rotação do sapato entre calcanhar e ponta.
- Seis clipes sintéticos novos de parquet, com camadas de calcanhar/sola. São síntese procedural, não gravações reais.
- Sem repetição consecutiva do mesmo clipe; pequenas variações de volume, altura e posição estéreo. Ganho aplicado uma vez, corrigindo a multiplicação duplicada anterior.
- Movimento real e contato com o chão determinam a passada. Parada e teleporte não disparam passos. Nenhum balanço foi aplicado à câmera.

Validação: 23 verificações no Editor aprovadas, incluindo contato/áudio 1:1, alternância de pernas, apoio sem deslizamento mensurável no teste, solas acima do piso, parada, teleporte e liberação de recursos. Sem teste em aparelho/visor físico.

`caminhada.mp4`: captura real da animação, sem áudio, 3 segundos a 30 fps.
`passos-preview.wav`: sequência demonstrativa das seis variações, sem reverberação do ambiente; não é captura do áudio do jogo.
`Museu-Passos-Pernas.apk`: versão Android atualizada.

Gerar os clipes: `MuseumModerna.MuseumAudioSetup.ApplyParquetFootsteps`.
Validar: `MuseumModerna.BodyValidation.Run`, sem `-quit`.
Para gravar a caminhada, definir `MUSEUM_RECORD_WALK=1` e `MUSEUM_CAPTURE_OUTPUT`.
