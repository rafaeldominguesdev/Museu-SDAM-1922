# Visita com celular em visor VR Box

Alvo configurado: Android 8 ou superior, ARM64, celular com giroscópio e visor com
perfil de lentes compatível com Cardboard. A câmera possui rastreamento de rotação
(3DoF). A locomoção é virtual; o visor não rastreia passos físicos.

## Antes de encaixar o celular

1. Abra o aplicativo com o celular na horizontal.
2. Escolha **Modo VR** e aguarde a preparação do visor.
3. Encaixe o celular no visor e olhe à frente.

O app usa o perfil de lentes salvo no Cardboard quando disponível e não exige QR
para entrar. **Modo normal** permite visitar em 2D. Nas opções desse modo,
**Preparar visor VR** volta à preparação.

## Dentro do visor — sem tocar na tela

- Olhe para uma peça: sua ficha aparece em espaço 3D, visível pelos dois olhos.
- Mova a cabeça para colocar a mira sobre um botão por **1 segundo**: o círculo
  se completa e aciona o botão. O controle acompanha a cabeça, não os olhos.
  Manter o olhar não repete a ação. Desvie e volte para acioná-lo novamente.
- **Anterior** e **Próxima** percorrem as páginas; não é necessário arrastar texto.
- **Fixar**, **Liberar** e **Fechar** também funcionam pelo olhar.
- Sem ficha aberta, baixe a cabeça para mostrar **Caminhar** e **Menu**. A barra
  some ao olhar à frente e não acompanha o giro enquanto você mira nos botões.
- No modo padrão, mire em **Caminhar** por 1 segundo e levante a cabeça para
  iniciar o movimento. Baixe a cabeça para parar. O botão também funciona se a
  preferência de caminhada por inclinação estiver ativada.
- **Menu** (ou **Opções** na ficha) abre os ajustes. Olhar o céu não abre opções.
  **Voltar** fecha as opções sem sair da visita.
- A caminhada opcional por inclinação é ativada nas opções: olhe à frente para
  armar o gesto, baixe para andar e levante para parar. O ritmo em VR é limitado
  a 1,1 m/s. Menu aberto ou olhar sobre a ficha suspendem o caminhar.
- O menu permite ampliar texto, resumir, mudar contraste, desativar fichas,
  reduzir animações, prolongar leitura, centralizar a visão e sair do VR.

A ficha fica no lugar ao girar a cabeça: seus controles podem ser alcançados pela
mira. Há 1,2 segundo de tolerância ao desviar de uma obra para alcançar a ficha
(ou 5 segundos com leitura longa). A interface funciona como sobreposição em 3D;
as paredes continuam bloqueando a detecção das obras.

## Implementação

O app usa o SDK oficial Google Cardboard XR Plugin, fixado na **tag v1.30.1**
(o package.json do fornecedor ainda informa 1.30.0). Essa tag antecede a remoção
do suporte ao Unity 2022.3 na versão 1.31.0. O SDK faz a renderização por olho,
a correção óptica e a leitura da pose. Não se trata de duplicar uma imagem 2D.

- `MobileVrMode`: preparação, QR, inicialização manual de XR e entrada/saída.
- `VrWorldGuide`: Canvas em espaço 3D, paginação e controles pelo olhar.
- `GazeActivation`: temporização e bloqueio de repetição dos botões.
- `GyroscopeController`: usa o SDK para a pose durante VR, sem somar duas rotações.
- `MobileVrSetup.Apply`: Android landscape, OpenGLES3, IL2CPP/ARM64, API26–35,
  Input Handling Both, dependências Gradle e loader Cardboard.

O formato original 2D continua disponível. A prévia do Editor testa interação;
somente o aparelho Android valida rastreamento nativo, renderização estereoscópica,
ajuste às lentes, latência e conforto de leitura. Não há suporte iOS configurado.

## APK e testes

APK com correções de caminhada, opções e retomada de fichas, gerado e verificado em
5 de outubro de 2026:
[Museu-Controles-VR-Corrigidos-2026-10-05.apk](../artifacts/controle-vr-2026-10-05/Museu-Controles-VR-Corrigidos-2026-10-05.apk)
(133 MiB). Transfira para o celular Android e abra o arquivo para instalar.
Se o Android solicitar, autorize a instalação pelo aplicativo usado para abrir o APK.
Depois siga a preparação do visor no início deste guia.

Passaram 226 verificações de VR, 148 de menus/toque, 43 de movimento e a regressão
das 28 fichas. Veja o [relatório de correções](../artifacts/controle-vr-2026-10-05/LEIA-ME.md),
incluindo os limites dos testes sem aparelho físico.

No Unity: **Museum > VR Box > Gerar APK Android**.
O resultado é gravado em `artifacts/MuseumVR_Build/MuseudaSemanaArteModerna.apk`
na raiz do repositório.

Teste de interação no Editor, a partir da raiz do repositório:

```sh
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod MuseumModerna.VrValidation.Run \
  -logFile /tmp/museum-vr-validation.log
```

O teste encerra o Unity automaticamente; não use `-quit`. Verifica dwell,
paginação, leitura, fixação, menu e saída sem simular toques ou invocar cliques.

Fontes técnicas: [Cardboard v1.30.1](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.30.1),
[alteração de compatibilidade em v1.31.0](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.31.0),
[SDK1.30 e dependências Android](https://github.com/googlevr/cardboard/blob/v1.30.0/sdk/build.gradle),
[guia oficial Cardboard](https://developers.google.com/cardboard/develop/unity/quickstart).
O guia atual refere-se a Unity6; o projeto usa a versão compatível fixada acima.
