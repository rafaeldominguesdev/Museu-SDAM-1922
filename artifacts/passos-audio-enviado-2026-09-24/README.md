# Passos com o áudio enviado

Fonte: MP3 de 5 segundos fornecido pelo usuário em 24/09/2026, cujo nome começa com `ElevenLabs_Passos_suaves_de_corrida`. O arquivo original em Downloads não foi modificado.

Seis recortes de 240 ms, iniciados em 1,10; 1,37; 1,64; 2,14; 2,40 e 2,67 segundos. Conversão para mono PCM 16-bit/44,1 kHz, entrada de 4 ms, saída de 35 ms, ganho de 6 e limitador a 0,8 para evitar saturação. Sem alteração da velocidade da gravação.

Arquivos: `UnityProject/Assets/Audio/UserStep_01.wav` a `UserStep_06.wav`. Substituem os sons sintéticos na configuração da cena; os arquivos anteriores foram preservados. Cada contato da animação dispara um recorte, sem repetição consecutiva.

Reaplicar: menu **Museum > Audio > Aplicar passos enviados pelo usuario**, ou método `MuseumModerna.MuseumAudioSetup.ApplySuppliedFootsteps`.

APK: `Museu-Passos-Audio-Enviado.apk`.

Validação: 23 verificações de corpo e sincronização aprovadas no Editor; compilação Android concluída e assinatura do APK verificada. Sem teste em visor físico.
