# CSC_53433_EP - Creative and Generative Models in Computer Graphics (2026-2027) - Lab Course

[Github Repository](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027) | [Github Page](https://paul-hubert.github.io/CSC_53433_EP-2026-2027/) | [EvoSim documentation](https://paul-hubert.github.io/CSC_53433_EP-2026-2027/evosim/)

> **Course redesign in progress (2026–2027):** the Crowds & Evolution lab is being
> reworked around evolving *text-prompt genomes* whose behaviour is decided by an LLM.
> It becomes **Lab 1**, on a flat world; the terrain and foliage labs follow.
> Documentation: [`Docs/prompt-genome/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/prompt-genome/README.md) ·
> design history: [`Docs/redesign/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/redesign/README.md) · current state:
> [`Docs/redesign/09-progress-log.md`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/redesign/09-progress-log.md) · Python
> prototype: [`prototype/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/prototype/README.md) · contract and proposed Unity architecture:
> [`Docs/Interface/`](https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027/blob/master/Docs/Interface/README.md).

------

- [Course organisation](#Courseorganisation)
	- [Teaching assistants](#Teachingassistants)
	- [Schedule](#Schedule)
	- [Labs](#Labs)
- [Before the first session](#Beforethefirstsession)
- [Opening the project](#Openingtheproject)


------

<a name="Courseorganisation"></a>
## Course organisation

Welcome to the lab sessions of CSC_53433_EP "Creative and Generative Models in Computer Graphics" for the course 2026-2027: programming modules in Unity.

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

**Please do this at home:** the network in the Turing building can't handle everyone downloading Unity at once.

1. Install [Unity Hub](https://unity.com/download), the launcher that manages your Unity versions and projects.
2. Create a Unity account: the Hub asks for it on launch. The free personal plan is enough.
3. In the Hub, install the Unity **6.3** editor (`6000.3.9f1`, the version the project is saved with): `Installs > Install Editor`.
4. Install a text editor configured for C#. [Visual Studio Code](https://code.visualstudio.com/Download) is a popular option, but any code editor or IDE works.
5. Clone the repository: `git clone https://github.com/Paul-Hubert/CSC_53433_EP-2026-2027`
6. Check that Unity launches and that your graphics drivers work: open the project once (see below).
7. Bring a mouse during the sessions!

------

<a name="Openingtheproject"></a>
## Opening the project

1. In Unity Hub, click `Add > Add project from disk`.
2. Select the folder you cloned: it contains folders such as `Assets` and `Packages`.
3. Open the project with Unity 6.3.
4. In the project, open "SampleScene". That's all!

Then follow the instructions in the Lab 1 presentation.
