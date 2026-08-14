# PCQA_Test

**PCQA_Test** is a research prototype developed using the **Unity Engine** for the **visualization and subjective quality assessment of Dynamic Point Clouds (DPCs)** in **Virtual Reality (VR)** environments.

The project was designed to support experimental workflows related to **Point Cloud Quality Assessment (PCQA)**, including point cloud rendering, DPC sequence playback, user interaction, and subjective score collection in a controlled VR environment.

This repository contains the essential Unity project structure required to open, inspect, run, and continue developing the experimental application.

---

## Project Objective

The main objective of this project is to provide a functional Unity environment for experiments involving the **subjective quality assessment of dynamic point clouds** in virtual reality.

The current project structure supports the development and execution of workflows involving:

- point cloud loading and rendering;
- visual presentation of point cloud sequences;
- experimental scene control;
- voting and response collection;
- runtime interaction and user interface management;
- management of assets, shaders, and VR-related settings.

---

## Default Software Configuration

The application was developed and configured using the following software and versions:

| Software or Component | Version / Parameter |
|---|---:|
| Unity Engine | 2019.4.40f1 |
| [Pcx](https://github.com/keijiro/Pcx) | 0.1.5 |
| Android NDK | r16b |
| Android SDK | 26.1.1 |
| JDK | 1.8.0 |

> **Important:** Use **Unity 2019.4.40f1** to reduce the risk of incompatibilities with the project's packages, scripts, shaders, and settings.

---

## Usage Tutorial

### 1. Clone the Repository

Run the following command in a terminal:

```bash
git clone https://github.com/josefoberdan/PCQA_Test.git
```

Then access the project directory:

```bash
cd PCQA_Test
```

### 2. Open the Project in Unity Hub

1. Start **Unity Hub**.
2. Select **Open** or **Add project from disk**.
3. Select the root directory of the cloned repository.
4. Open the project using **Unity 2019.4.40f1**.

### 3. Prepare the Experiment

1. Open the relevant experimental scene from:

   ```text
   Assets/Scenes/
   ```

2. Add or organize the DPC sequences in:

   ```text
   Assets/StreamingAssets/PointCloudSequence/
   ```

3. Check all runtime resources and object references in the Unity **Inspector**.
4. Verify the scripts, user interface components, assets, shaders, and VR settings required by the experiment.

### 4. Run the Application

The application can be:

- executed directly in the Unity Editor;
- compiled for the target VR device;
- deployed to compatible devices such as the **Meta Quest** or **HTC Vive**.

Before building the application, ensure that the packages, drivers, and platform-specific settings required by the target device are correctly configured.

### 5. Conduct the Experiment

Use the experimental interface to:

1. load the DPC sequence;
2. present the visual stimulus to the participant;
3. allow the participant to inspect the dynamic point cloud in VR;
4. collect the participant's subjective quality score;
5. continue the process according to the configured experimental sequence.

---

## Citation

If you use **PCQA_Test** in an academic study, please cite the following publication:

```bibtex
@inproceedings{silva2026perceptual,
  author    = {Silva, Josef Augusto Oberdan Souza and
               Rehbein, Gustavo and
               Borda, Adriane and
               Porto, Marcelo and
               Corr{\^e}a, Guilherme},
  title     = {Perceptual Analysis of Compressed Dynamic Point Clouds
               in Virtual Reality Environments},
  booktitle = {Anais do LIII Semin{\'a}rio Integrado de Software e
               Hardware (SEMISH 2026)},
  year      = {2026},
  pages     = {494--505},
  publisher = {Sociedade Brasileira de Computa{\c{c}}{\~a}o},
  address   = {Porto Alegre},
  issn      = {2595-6205},
  doi       = {10.5753/semish.2026.22376},
  url       = {https://doi.org/10.5753/semish.2026.22376},
  note      = {Accessed on: August 14, 2026}
}
```

### Publication Link

[Perceptual Analysis of Compressed Dynamic Point Clouds in Virtual Reality Environments](https://sol.sbc.org.br/index.php/semish/article/view/43535)

---

## Related Resources

- [Pcx — Point Cloud Importer and Renderer for Unity](https://github.com/keijiro/Pcx)
