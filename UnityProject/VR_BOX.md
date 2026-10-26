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

- Aproxime-se até 3 metros e olhe para uma peça por pelo menos 0,55 segundo:
  sua ficha aparece em espaço 3D, visível pelos dois olhos.
- Mova a cabeça para colocar a mira sobre um botão por **1 segundo**: o círculo
  se completa e aciona o botão. O controle acompanha a cabeça, não os olhos.
  Manter o olhar não repete a ação. Desvie e volte para acioná-lo novamente.
- **Anterior** e **Próxima** percorrem as páginas; não é necessário arrastar texto.
- A ficha tem apenas **Anterior** e **Próxima página**. Desvie o olhar para
  encerrar a leitura após a tolerância configurada.
- Olhe à frente para preparar o gesto; baixe a cabeça para caminhar e levante
  para parar imediatamente. Não é necessário solicitar a caminhada por botão.
- Sem ficha aberta e a mais de 4 metros dos expositores, **Menu** fica ao lado
  esquerdo. Está disponível no centro do hall e em pontos livres das salas.
  Ele permanece estável ao mirar nos controles e desaparece ao olhar para baixo.
- **Menu** abre os ajustes. Olhar o céu não abre opções.
  **Voltar** fecha as opções sem sair da visita.
- **Menu inicial**, nos ajustes, ou **Voltar** do Android durante a visita abre
  a escolha de modo dentro do visor, com os créditos de autoria e controles pelo
  olhar. **Continuar em VR** retoma a visita; **Modo normal** encerra o visor e
  continua em 2D. Na tela inicial VR, **Voltar** também retoma a visita.
- A caminhada por inclinação é sempre usada no VR, inclusive com preferências
  antigas salvas. Velocidades: lenta 0,8 m/s, normal 1,25 m/s e rápida 1,6 m/s.
  Menu aberto ou olhar sobre a ficha suspendem o caminhar. Após pausa ou opções,
  olhe à frente antes de iniciar outro gesto.
- O menu permite ampliar texto, resumir, mudar contraste, desativar fichas,
  reduzir animações, prolongar leitura, centralizar a visão e sair do VR.

A ficha abre à frente do visitante, centralizada na direção do olhar, mesmo que
sobreponha a obra. Permanece fixa no espaço ao andar ou girar a cabeça: aproximar-se
reduz a distância de leitura e preserva a página. Há 1,5 segundo de tolerância ao desviar
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

APK com acervo reorganizado, fichas por proximidade e caminhada por inclinação,
gerado e verificado em 22 de outubro de 2026:
[Museu-Curadoria-VR.apk](../artifacts/curadoria-2026-10-21/Museu-Curadoria-VR.apk)
(149,38 MiB). Transfira para o celular Android e abra o arquivo para instalar.
Se o Android solicitar, autorize a instalação pelo aplicativo usado para abrir o APK.
Depois siga a preparação do visor no início deste guia.

Passaram 360 verificações de acervo, 254 de VR, 148 de menus/toque, 43 de movimento
e seis cenários de seleção pelo olhar. As 31 fichas foram verificadas nos três
tamanhos de texto. Veja o [relatório da entrega](../artifacts/curadoria-2026-10-21/README.md),
incluindo a assinatura e integridade do APK e os limites sem aparelho físico.

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
paginação, leitura, proximidade, menu e saída pelo olhar.

Fontes técnicas: [Cardboard v1.30.1](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.30.1),
[alteração de compatibilidade em v1.31.0](https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.31.0),
[SDK1.30 e dependências Android](https://github.com/googlevr/cardboard/blob/v1.30.0/sdk/build.gradle),
[guia oficial Cardboard](https://developers.google.com/cardboard/develop/unity/quickstart).
O guia atual refere-se a Unity6; o projeto usa a versão compatível fixada acima.
