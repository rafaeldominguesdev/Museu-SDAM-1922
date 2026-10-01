# Fontes e escolhas curatoriais

## Textos

- O catálogo ativo é `UnityProject/Assets/Resources/MuseumThematicCatalog.json`. Cada uma das 26 fichas contém texto didático original, período, relação com 1922 e links de referência exibidos na ficha interativa.
- Contexto, Semana e Mário: [Biblioteca Nacional — Semana de Arte Moderna](https://brasilianafotografica.bn.gov.br/?p=26620) e [BN Digital — O Modernismo](https://bndigital.bn.gov.br/dossies/rede-da-memoria-virtual-brasileira/artes/o-modernismo/).
- Oswald: [BN Digital](https://bndigital.bn.gov.br/oswald-de-andrade/), [Biblioteca Mário de Andrade](https://www.prefeitura.sp.gov.br/web/cultura/w/bibliotecas/noticias/30801), exemplares da BBM-USP de [Primeiro caderno, 1927](https://digital.bbm.usp.br/handle/bbm/8642) e [Serafim Ponte Grande, 1933](https://digital.bbm.usp.br/handle/bbm/8718).
- Manifestos: [Pau-Brasil](https://prefeitura.sp.gov.br/web/cultura/w/bibliotecas/noticias/9997), [Revista de Antropofagia](https://bndigital.bn.gov.br/revista-de-antropofagia/) e [Glossário da literatura brasileira, FUNAG](https://funag.gov.br/biblioteca-nova/produto/loc_pdf/1232/1/glossario_da_literatura_brasileira_para_leitores_estrangeiros).
- As datas de 1924, 1928 e 1929 são explicitamente posteriores à Semana. O livro *Pau Brasil* (1925) fica separado do manifesto (1924).
- Tarsila e *Abaporu* aparecem no contexto da Antropofagia. Não foi criada uma sexta sala; a lista principal do pedido tem quatro artistas. Sua ausência da Semana de 1922 é contextualizada, sem apagar sua importância posterior.

## Reproduções

Os caminhos abaixo são relativos a `UnityProject/Assets/Artworks/Images`. Imagens incorporadas nos PDFs foram extraídas sem redesenho, corte ou alteração de cores. A proporção original é preservada na cena; a importação do Unity usa mipmaps e ASTC 6×6 no Android. Não há capas de livro inventadas: os livros recebem painéis didáticos identificados como tais.

| Arquivo | Obra | Origem da reprodução |
|---|---|---|
| `homem_amarelo.jpg` | O homem amarelo, 1915–1916 | Imagem existente, fotografia Campos e Kajiya, IEB-USP; registro em `artifacts/artwork-sources/anita/manifest.json` |
| `tropical.png` | Tropical, 1917 | Imagem existente, fotografia Campos e Kajiya, Pinacoteca; mesmo registro anterior |
| `anita_boba.jpg` | A boba, 1915–1916 | Catálogo MAM-SP, página 67 do PDF, imagem Im0; acervo MAC-USP |
| `anita_farol.jpg` | O farol, 1915 | Catálogo MAM-SP, página 67 do PDF, imagem Im2; Coleção Gilberto Chateaubriand, MAM Rio |
| `di_meninas.jpg` | Meninas cariocas, 1926 | Catálogo Almeida & Dale, página 61 do PDF, imagem Im0; coleção particular |
| `di_mulheres.jpg` | Mulheres protestando, 1941 | Catálogo Almeida & Dale, página 101 do PDF, imagem Im1; coleção particular |
| `di_pescadores.jpg` | Pescadores, 1946 | Catálogo Almeida & Dale, página 89 do PDF / 176 impressa, imagem Im0; coleção particular |
| `di_violoes.jpg` | Moças com violões, 1937 | MAM Rio no Google Arts & Culture; Coleção Gilberto Chateaubriand |

- [Catálogo MAM-SP — Moderno onde? Moderno quando?](https://mam.org.br/wp-content/uploads/2022/04/mam-modernoondemodernoquando-catalogo-com-ad.pdf).
- [Catálogo Almeida & Dale — Di Cavalcanti: Conquistador de lirismos](https://almeidaedale.com.br/wp-content/uploads/2025/04/2016_emiliano_di_cavalcanti_conquistador-de-lirismo.pdf).
- [Moças com violões — MAM Rio / Google Arts & Culture](https://artsandculture.google.com/asset/girls-with-guitars-emiliano-di-cavalcanti/SgFLljjz6G_Ujg). Imagem de 1200 × 970 px obtida da reprodução publicada nessa página.
- [Museu do Senado — documentação de Moças com violões](https://tainacan.senado.leg.br/acervo/mocas-com-violoes-reproducao/). A fotografia da reprodução emoldurada do Senado não foi usada como textura.
- *Pescadores* é um título de diferentes obras do artista. Foi selecionada a versão de 1946, óleo sobre tela, 33 × 46 cm, identificada no catálogo e na ficha. A seleção pode ser substituída se o grupo fornecer uma referência visual diferente.
- Os créditos de acervo e reprodução constam nas legendas e fichas. A disponibilidade pública das fontes não é uma declaração de autorização para distribuição comercial; conferir permissões antes de publicação pública/comercial do aplicativo.

## SHA-256 das seis novas imagens

```text
733db21d375e0efc73b8c04e984669db02030b8874f3fb1517dbaa13edf847b5  anita_boba.jpg
83aa636b7bc0dcdbc6df97d0827db1b2c1c96deb5f643f14a45ff4d392ff0758  anita_farol.jpg
7f9781d8aebc9ff97147b994078d06c89e4aaf29fe4f1318b800af94175e63f9  di_meninas.jpg
be9d2b313dfa77a46094ebe7d65b4af9dd102098f4ad9927d15fad69b71f8c67  di_mulheres.jpg
4f5a31cbd02883f9abfa17f866e47fd996bb5df3c18c288a731f97b2aad29296  di_pescadores.jpg
a37efe92a275e5af4d9168b2431665a8259f8eab1a50da33f540b8a3de170ff2  di_violoes.jpg
```
