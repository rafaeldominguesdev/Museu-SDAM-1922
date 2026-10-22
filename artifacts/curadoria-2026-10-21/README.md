# Acervo reorganizado e controles VR

Entrega iniciada em 21/10 e validada em 22/10/2026, com Unity 2022.3.62f1.

## Resultado

- 31 fichas: 12 pinturas, oito livros, três manifestos, cinco biografias e três painéis de contexto.
- Manifestos na antiga sala lateral de Oswald; literatura de Oswald preservada na sala ao fundo.
- Tarsila com A Negra, A Cuca, Abaporu e Operários. Pescadores de 1951 substitui a versão de 1946 na exposição ativa.
- Fichas VR exigem proximidade de até 3 m e olhar sustentado. Reposicionam-se quando o visitante se desloca, preservando a página e a fixação. Girar apenas a cabeça não desloca os botões.
- Caminhada VR ao olhar para baixo; levantar a cabeça interrompe imediatamente. Velocidades lenta/normal/rápida: 0,8 / 1,25 / 1,6 m/s. Opções em botão lateral, sem solicitação de caminhada.
- Preferências antigas do modo normal continuam disponíveis, sem desativar a caminhada por inclinação no VR.

[Fontes e análise das obras](FONTES.md) · [Capturas das salas](salas-contato.jpg).

## Verificação

| Validação | Resultado |
| --- | --- |
| ThematicValidation.ApplyAndRun | 360 verificações; cena salva e renderizada |
| VrValidation.Run, cena final | 254 verificações de execução e seis cenários de dwell |
| MovementValidation.Run | 43 verificações |
| AndroidMenuValidation.Run | 148 verificações em sete resoluções |
| Imagens | Formato, dimensões e conteúdo visual conferidos |
| APK | Build concluído; CRC ZIP, alinhamento e assinatura v2 aprovados |

As verificações VR incluem preferências antigas, caminhada por inclinação, parada ao levantar a cabeça, pausa, retorno do aplicativo, colisões, proximidade, deslocamento durante leitura, paginação e fixação. Todas as 31 fichas foram verificadas nos três tamanhos de texto.

As capturas completas e logs ficam nos subdiretórios locais `salas`, `vr`, `controles` e nos arquivos `.log`, excluídos do versionamento. A montagem das salas é versionada como evidência visual resumida. O PDF consultado da Revista USP é um arquivo temporário local; a imagem extraída e sua procedência estão no projeto.

## APK Android

[Museu-Curadoria-VR.apk](Museu-Curadoria-VR.apk), gerado em 22/10/2026: 156.631.816 bytes (149,38 MiB). Android 8 ou superior (API 26), ARM64, alvo API 35. Pacote `com.SemanaArteModerna.MuseudaSemanaArteModerna`, versão 1.1.0, código 2.

SHA-256: `b79435d88c62867b1c4d12d8aa02a11a882242b572f45980f559586684b7abf8`.

O Unity encerrou com código zero e `BUILD OK`. Foram conferidos os CRCs das 1.228 entradas ZIP, alinhamento com `zipalign -c 4` e assinatura com `apksigner verify --verbose`. O APK fica disponível localmente e não é versionado no Git.

## Reprodução no Windows

Na raiz do repositório, executar um comando por vez com o projeto fechado no Editor:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe'
$env:MUSEUM_CAPTURE_OUTPUT = "$PWD/artifacts/curadoria-2026-10-21/salas"
& $unity -batchmode -projectPath "$PWD/UnityProject" -executeMethod MuseumModerna.ThematicValidation.ApplyAndRun -logFile "$PWD/artifacts/curadoria-2026-10-21/thematic.log" | Out-Host

$env:MUSEUM_CAPTURE_OUTPUT = "$PWD/artifacts/curadoria-2026-10-21/vr"
& $unity -batchmode -projectPath "$PWD/UnityProject" -executeMethod MuseumModerna.VrValidation.Run -logFile "$PWD/artifacts/curadoria-2026-10-21/vr-final.log" | Out-Host

$env:MUSEUM_APK_OUTPUT = "$PWD/artifacts/curadoria-2026-10-21/Museu-Curadoria-VR.apk"
& $unity -batchmode -quit -projectPath "$PWD/UnityProject" -executeMethod BuildScript.BuildAndroid -logFile "$PWD/artifacts/curadoria-2026-10-21/build-android-final.log" | Out-Host
```

Os validadores encerram o Unity; não adicionar `-quit` aos testes. A geração do APK usa o SDK, NDK e OpenJDK instalados pelo Unity Hub.

## Limites

Testes e capturas no Editor não comprovam rastreamento no Android, conforto, renderização binocular nas lentes, FPS ou aquecimento. A velocidade rápida precisa ser avaliada no visor físico. A reprodução institucional de Operários tem 871 × 633 pixels; sua resolução original foi preservada.
