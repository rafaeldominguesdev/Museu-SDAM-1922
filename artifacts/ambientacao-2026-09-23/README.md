# Ambientação museológica — 23/09/2026

## Entrega

- Três cordões protegem seis pinturas: parede do fundo, Lasar Segall e John Graz.
- Treze postes de bronze escuro com bases torneadas, cordas vinho curvas e retornos laterais.
- Limites físicos impedem atravessar os cordões com o CharacterController do visitante.
- Oito vasos discretos, usando a vegetação já disponível no projeto. A cena atual não continha o grupo documentado anteriormente; os vasos foram instalados sem restaurar as ilhas de exposição antigas.
- Banho quente simulado sob as cornijas, por malhas de gradiente aditivo; não é iluminação global calculada. Zero luzes adicionais em tempo real.
- Sombras de contato nos apoios e ocultação da geometria demonstrativa que cobria pinturas já texturizadas.
- Cena persistida em `UnityProject/Assets/Scenes/MuseumScene.unity`.

O gerador `MuseumAmbientDetails.Apply` é reaplicável e também participa de `LouvreAtmosphereSetup.ApplyTo`. O menu é **Museum > Visual > Aplicar cordoes e luz de cornija**. Os novos detalhes não dependem do bake de oclusão antigo.

## Verificação

| Verificação | Resultado |
|---|---|
| Geometria dos detalhes, vasos e sombras | 21.834 triângulos, valores finitos |
| Limites de colisão | 16 verificados |
| Locomoção contra barreiras | 3 testes com o CharacterController real |
| Olhar para quadros e vitrine | 25 fichas acessíveis |
| Circulação | 6 percursos livres, raio de 0,30 m |
| Vasos | 8, sem sobreposição com postes |
| Regressão VR | 32 verificações de interação e recuperação aprovadas no Editor |

Capturas reais mono do Editor: `galeria.png`, `cordoes.png`, `detalhe.png`, `plantas.png` e `entrada-e-plantas.png`. Não são imagens geradas por IA. A captura da entrada mostra o eixo central; os vasos laterais aparecem em `plantas.png`.

O aparelho Android/visor físico não foi testado. Não há medição de FPS ou conforto estereoscópico nesta entrega.

APK desta versão: `Museu-Ambientacao.apk`, separado dos APKs anteriores.

Compilação concluída: aproximadamente 91 MB; assinatura APK v2 verificada com `apksigner`. Pacote `com.SemanaArteModerna.MuseudaSemanaArteModerna`, versão 1.1.0 (2), Android API mínima 26, alvo 35.

SHA-256: `508d4ef5673ad6f470535066132bcd3469e7c749a005a8900c5da13f9214da79`.

## Reproduzir as verificações

Executar Unity 2022.3.62f1 em batch com o projeto `UnityProject`:

1. `-quit -executeMethod MuseumModerna.MuseumAmbientDetails.Apply`
2. `-executeMethod MuseumModerna.AmbientDetailsValidation.Run` (sem `-quit`; o teste encerra o processo)
3. `-executeMethod MuseumModerna.VrValidation.Run` (sem `-quit`)
4. Definir `MUSEUM_APK_OUTPUT` e executar `-quit -executeMethod BuildScript.BuildAndroid`.
