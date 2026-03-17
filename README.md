# PCQA_Teste

O **PCQA_Teste** é um protótipo de pesquisa desenvolvido na _engine_ **Unity** para a **visualização e avaliação subjetiva da qualidade de nuvens de pontos dinâmicas (DPC)** em ambientes de **Realidade Virtual (VR)**. O projeto foi concebido para dar suporte a fluxos experimentais relacionados à **Point Cloud Quality Assessment (PCQA)**, incluindo renderização de nuvens de pontos, sequenciamento de DPCs, interação do usuário e coleta de pontuações subjetivas em um ambiente controlado de VR.

Este repositório contém a estrutura essencial do projeto Unity necessária para abrir, inspecionar e continuar o desenvolvimento da aplicação experimental.

---

## Objetivo do projeto

O principal objetivo deste projeto é fornecer um ambiente funcional em Unity para experimentos envolvendo a **avaliação subjetiva da qualidade de nuvens de pontos** em realidade virtual.

A estrutura atual oferece suporte ao desenvolvimento e à execução de fluxos como:

- carregamento e renderização de nuvens de pontos;
- apresentação visual de sequências de nuvens de pontos;
- controle de cenas experimentais;
- votação e coleta de respostas;
- interação em tempo de execução e manipulação de interface;
- gerenciamento de assets, shaders e configurações relacionadas à VR.

---

## Configuração padrão de software

A aplicação foi desenvolvida e configurada com a seguinte pilha de software:

| Software | Versão / Parâmetro |
|---|---|
| Unity Engine | 2019.4.40f1 |
| PCX (https://github.com/keijiro/Pcx) | 0.1.5 |
| NDK | r16b |
| SDK | 26.1.1 |
| JDK | 1.8.0 |

---

## Tutorial para Uso:

**1. Clone o repositório**
- git clone https://github.com/josefoberdan/PCQA_Test.git

**2. Abra o projeto no Unity Hub**

- Inicie o Unity Hub

- Selecione Open

- Escolha a pasta raiz deste repositório

**3. Utilize a versão correta do Unity**

- Unity 2019.4.40f1

---

## Estrutura do repositório

```text
PCQA_Teste/
├── Assets/
├── Packages/
└── ProjectSettings/
