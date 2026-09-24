# Pinturas mais claras

As nove pinturas passaram a usar seus próprios materiais neutros, em vez do material bege do suporte. O preenchimento emissivo utiliza a própria imagem, com intensidade 0,55, preservando as áreas escuras da textura e reduzindo a dependência da luz ambiente. Nenhuma luz adicional foi criada.

Correção persistida na cena e integrada aos geradores `ArtworkTextureSetup` e `MuseumAmbientDetails`. Não foram alteradas a geometria, as posições ou as interações das pinturas.

Verificação: comparação visual das capturas reais do Editor, 25 fichas acessíveis, seis percursos livres e três testes físicos contra as barreiras aprovados. Consulte `validacao.txt`. O visor físico não foi testado.

- Antes: `../ambientacao-2026-09-23/galeria.png`
- Depois: `galeria.png`
- APK: `Museu-Quadros-Iluminados.apk`
