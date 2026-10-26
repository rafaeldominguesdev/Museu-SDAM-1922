# Créditos na tela nativa de carregamento

## Correção

A autoria estava somente na tela de escolha dos modos. A configuração nativa de abertura tinha `m_SplashScreenLogos: []`, exibindo apenas a marca Unity antes de entrar na cena.

`AndroidBrandingSetup.Apply`, já chamado pelo build Android, agora registra `MuseumAuthorsSplash.png` como sprite inteiro por 4 segundos. Os nomes aparecem centralizados, com a marca Unity abaixo, em fundo escuro e sem zoom. As configurações também foram salvas no projeto.

A imagem é gerada a partir de `WelcomeScreen.Authorship`, mantendo os nomes completos usados no menu. Para regenerá-la no Windows com Pillow instalado, executar da raiz:

```powershell
python artifacts/splash-creditos-2026-10-25/gerar-creditos.py
```

Arte tipográfica conferida: [créditos](../../UnityProject/Assets/Branding/MuseumAuthorsSplash.png).

## Verificação

- Gerador: verifica duas linhas de nomes, margens sem cortes, PNG RGBA e transparência.
- `MuseumModerna.SplashValidation.Run`: aprovado no Unity 2022.3.62f1; confirma sprite correto, imagem inteira sem recorte, transparência, duração mínima e marca Unity abaixo. Log local: `validacao.log`.
- A marca e a duração usam a [configuração nativa de Splash Image do Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/class-PlayerSettingsSplashScreen.html). O tempo total inclui também a transição final do motor.

Nenhum dispositivo estava conectado via ADB. A aparência e o tempo efetivo de abertura no Android ainda precisam ser conferidos no aparelho; a imagem acima é o asset, não uma captura da inicialização.

## APK corrigido

[Museu-Creditos-Carregamento.apk](Museu-Creditos-Carregamento.apk): 156.664.266 bytes. SHA-256: `d00e587812238cc1d7953e0da8111b9908fcc2e99170b6a6eafa41b9bf0e048d`.

Build com `BUILD OK` e encerramento do Unity com código zero (`build.log`). CRC das 1.231 entradas ZIP, alinhamento e assinatura Android v2 aprovados. A presença de `MuseumAuthorsSplash` também foi confirmada nos dados `globalgamemanagers.assets.split*` extraídos do APK, que incluem os assets nativos da abertura.

O APK permanece local, excluído do versionamento pelas regras existentes.
