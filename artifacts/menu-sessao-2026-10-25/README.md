# Texto Extra Grande, menu por sessão e números das salas

## Comportamento

- O controle de tamanho passa por padrão, grande, maior e Extra Grande. No VR, o corpo das fichas usa 32, 36, 40 e 44; no modo normal, o quarto nível acrescenta 9 ao tamanho base de cada texto. A preferência continua salva pelo mecanismo existente.
- A primeira página de opções VR tem a ação **Não exibir mais nesta sessão**, com a explicação de que o menu fica oculto até reiniciar o aplicativo e as configurações são mantidas.
- A ação fecha os ajustes e oculta o botão lateral. O atalho do visor também respeita o bloqueio. Pausa, recuperação de foco e troca entre modo normal e VR não o removem.
- A ocultação fica em memória, compartilhada pela sessão do aplicativo. A inicialização do Unity limpa somente esse bloqueio; nenhuma preferência de leitura ou velocidade é apagada.
- As fichas das obras continuam abrindo pelo olhar, com Anterior e Próxima página e fechamento ao desviar o olhar. O menu inicial continua disponível pelo comando Voltar.
- Os números 01 a 06 são brancos. A cena salva recebeu somente as seis alterações de cor; o gerador também foi atualizado para preservar a correção em futuras reconstruções.

## Verificação no Unity 2022.3.62f1

- `MuseumModerna.VrValidation.Run`: **692 verificações**, mais seis cenários de dwell, aprovados com saída 0. Log: `vr-final.log`.
- A paginação foi conferida nas **31 fichas nos quatro tamanhos**, incluindo preservação do conteúdo e das fontes documentais. As 31 fichas também foram abertas com os ajustes ocultos.
- Abertura, avanço, retorno e fechamento de uma ficha foram exercitados pelo olhar depois de ocultar os ajustes. Pausa, retorno ao menu inicial, troca de modo e preservação das preferências também foram verificados.
- O reinício da sessão foi simulado chamando o método registrado na inicialização do Unity. Isso confirma a lógica de reativação no Editor, sem alegar um reinício físico do Android.
- `MuseumModerna.AndroidMenuValidation.Run`: **183 verificações** e sete resoluções, aprovadas com saída 0. O controle normal foi acionado por eventos de toque com raycast, percorrendo os quatro tamanhos. Log: `menu-normal.log`.
- `MuseumModerna.ThematicValidation.Run`: **361 verificações** aprovadas com saída 0 na cena final, incluindo os seis números brancos. Log: `tematica-final.log`.
- Os testes restauram as preferências que alteram. `git diff --check` aprovado.

## Capturas renderizadas

- [Menu e explicação da sessão](vr-menu-sessao.png)
- [Opção Extra Grande](vr-menu-extra-grande.png)
- [Ficha Extra Grande com ajustes ocultos](vr-ficha-menu-oculto.png)
- [Hall com o menu oculto](vr-hall-menu-oculto.png)
- [Número da sala em branco](placa-sala-numero.png)

São capturas mono renderizadas no Editor. Nenhum Android estava conectado ao ADB; instalação, reinício real, lentes, tracking e conforto no visor permanecem pendentes.

## APK

[Museu-Menu-Sessao.apk](Museu-Menu-Sessao.apk): **156.664.602 bytes** (149,41 MiB).

- Build Android concluído com `BUILD OK` e saída 0 (`build.log`).
- CRC das **1.231 entradas ZIP**, alinhamento de 4 bytes e assinatura Android v2 aprovados.
- Os textos Extra Grande e Não exibir mais nesta sessão, assim como os símbolos do controle de sessão, foram encontrados no metadata IL2CPP do APK.
- SHA-256: `1f9c5d978d62c6e086cb9dd05f53daba503ff6fb1f6f3a41a0723af8bfbaa7b9`.
- Dados da conferência: [Museu-Menu-Sessao.verification.json](Museu-Menu-Sessao.verification.json).

APKs anteriores preservados. A skill solicitada `trail-code` não foi localizada nos diretórios de skills e plugins disponíveis; não foi aplicada.

## Reprodução

Definir `MUSEUM_CAPTURE_OUTPUT` para esta pasta. Executar Unity com `-batchmode -projectPath <UnityProject> -executeMethod <método> -logFile <log>`, sem `-quit`, pois as validações encerram o Editor. Para conferir a cena sem regenerá-la, usar `MuseumModerna.ThematicValidation.Run`.

Para o APK, definir `MUSEUM_APK_OUTPUT` para um novo arquivo e usar `-batchmode -quit -executeMethod BuildScript.BuildAndroid`. O APK permanece local, excluído do versionamento pelas regras existentes.
