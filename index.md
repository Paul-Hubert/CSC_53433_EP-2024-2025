# CSC_53433_EP - Creative and Generative Models in Computer Graphics (2026-2027) - Lab Course

[Github Repository](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027) | [Github Page](https://paul-hubert.github.io/CSC_53433_EP-2026-2027/) | [EvoSim documentation](https://paul-hubert.github.io/CSC_53433_EP-2026-2027/evosim/)

> **Course redesign in progress (2026–2027):** the Crowds & Evolution lab is being
> reworked around evolving *text-prompt genomes* whose behaviour is decided by an LLM.
> It becomes **Lab 1**, on a flat world; the terrain and foliage labs follow.
> Documentation: [`Docs/prompt-genome/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/prompt-genome/README.md) ·
> design history: [`Docs/redesign/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/redesign/README.md) · current state:
> [`Docs/redesign/09-progress-log.md`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/redesign/09-progress-log.md) · Python
> prototype: [`prototype/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/prototype/README.md) · contract and proposed Unity architecture:
> [`Docs/Interface/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/Interface/README.md). The Unity instructions below are unchanged.

------

- [Course organisation](#Courseorganisation)
	- [Teaching assistants](#Teachingassistants)
	- [Schedule](#Schedule)
	- [Labs](#Labs)
- [Before the first session](#Beforethefirstsession)
- [First Steps](#Firststeps)
	- [Introduction](#Introduction)
	- [Installing Unity](#InstallingUnity)
	- [Opening the project](#Openingtheproject)


------

<a name="Courseorganisation"></a>
## Course organisation

Programming modules in Unity.

<a name="Teachingassistants"></a>
### Teaching assistants

- Paul Boursin (Labs 1 & 4)
- Amandine Legrand (Labs 2 & 3)
- Aryamaan Jain (all labs)

<a name="Schedule"></a>
### Schedule

Wednesdays 13:30–16:30, Alan Turing building, room Nicole-Reine Lepeaute (room 31 Edsger Dijkstra also available).

<a name="Labs"></a>
### Labs

| Lab | Date | Topic |
|---|---|---|
| Lab 1 | Nov 4 | Exploring the genetic evolution of creature behaviours. You will keep training your models until the project defenses on Dec 16. |
| Lab 2 | Nov 18 | Tools for designing and editing terrains (expressive and neural) |
| Lab 3 | Dec 2 | Creating and editing objects and their distribution on terrains (expressive and neural) |
| Lab 4 | Dec 9 | Creature gaits and interaction with their environment |

The labs are done in groups of 3 (8 groups in total). Each group will be assigned a thematic universe.

------

<a name="Beforethefirstsession"></a>
## Before the first session

- Install [Unity Hub](https://unity.com/download), then install the Unity 6 editor from the Hub (`Installs > Install Editor`). **Please do this at home:** the network in the Turing building can't handle everyone downloading it at once.
- Clone the repository: `git clone https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027`
- Create a Unity account.
- Install a text editor configured for C# (e.g., [Visual Studio Code](https://code.visualstudio.com/Download)).
- Bring a mouse during the sessions!

Details for each step are in [First Steps](#Firststeps) below.

------

<a name="Firststeps"></a>
## First Steps

<a name="Introduction"></a>
### Introduction

Welcome to the repository for the lab sessions of CSC_53433_EP "Creative and Generative Models in Computer Graphics" for the course 2026-2027! 

Here, you will find a brief introduction to install and setup Unity. 

Please follow the instructions in the LAB 1 presentation.

<a name="InstallingUnity"></a>
### Installing Unity

Link to download: https://unity.com/download

- Download Unity Hub. It's a explorer tool for your projects where you can have simultaneously different Unity builds.
- Create a Unity account: It will be required when you launch the program. You can select the free option for personal use.
- Choose your Unity build. For this lab, we will be using Unity **6.3** (`6000.3.9f1`, the version the project is saved with).
- Make sure you have a text editor installed and configured for C#. [Visual Studio Code](https://code.visualstudio.com/Download) is a popular option, but you can use any other code editor or IDE.
- Now you should be able to open Unity. Make sure that you can successfully launch it, that your graphic drivers are working or that you do not have any particular problem.

------

<a name="Openingtheproject"></a>
### Opening the project

- Clone this repository.
- Make sure you have Unity Hub and a Unity built installed (as we describe in *Introduction*)
- In Unity Hub, click on `Open > Add project from disk`
- Select the parent folder of the repository, containing the Unity project. You will differentiate it because it contains folders such as `Assets` or `Packages`.
- Open the project by selecting the installed built of Unity.
- In the project, open "SampleScene". That's all!
