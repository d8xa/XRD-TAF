# XRD-TAF

*X Ray Diffraction - Theoretical Absorption Factors*

A GUI app to compute theoretical xray absorption factors (for xray diffracted in a capillary) efficiently on the device's GPU. Made with C#/Unity.

### Collaborators

<table>
  <tr>
    <td align="center"><a href="https://github.com/AcediaKleea"><img src="https://avatars.githubusercontent.com/u/31656388?v=4?s=100" width="70px;" alt=""/><br /><sub><b>AcediaKleea</b></sub></a><br /></td>
    <td align="center"><a href="https://github.com/d8xa"><img src="https://avatars.githubusercontent.com/u/40372844?v=4?s=100" width="70px;" alt=""/><br /><sub><b>d8xa</b></sub></a><br /></td> 
    <td align="center"><a href="https://github.com/zzfab"><img src="https://avatars.githubusercontent.com/u/35594229?v=4?s=100" width="70px;" alt=""/><br /><sub><b>zzfab</b></sub></a><br /></td>   
  <tr>    
<table>

## Installation

Download the latest release [here](https://github.com/d8xa/XRD-TAF/releases/latest). Extract the zip file and put the folder wherever you want to run the app.

## How to use

### GUI
#### Main screen
##### Presets
The app uses presets to store all configurable parameters of the experiment setup. The main screen has input fields for all those parameters.   
To save or overwrite a preset, enter a name into the field _preset name_ and hit the _save_ button.  
To load an existing preset, enter the name and click _load_.

All presets are stored under `XRD-TAF/Presets`, in `.json` format and can be copied to other installations of the app. 

##### Modes
There are three experiment modes available:
* `Point`: Computes a single averaged absorption factor for a single path of the xray (diffracted horizontally by angle `2theta`).
* `Area`: Computes pointwise averaged absorption factors for all xray paths to the points on a detector screen.
* `Integrated`: For each horizontal diffraction angle `2theta`, a ring segment is projected onto a detector screen, starting from the intersection point of xray and detector. A sequence of `k` points is distributed uniformly over that ring segment. The averaged absorption factors for each point are then combined into an average over the whole ring segment.

##### Parameters
The GUI only displays the parameters relevant to the currently selected mode.  
Here's an overview of all parameters:

| Parameter                 | Mode | Input type      |  Unit | Description                                                                                                                     |
|---------------------------|------|-----------------|:-----:|---------------------------------------------------------------------------------------------------------------------------------|
| Experiment style          | All  | Dropdown        |   -   | The experiment setup mode to use for computation.                                                                               |
| Grid resolution           | All  | int             |   px  | The number of points along each axis of the rectangle representing the capillary cross section.                                 |
| Total diameter            | All  | float           |   mm  | The total diameter of the capillary.                                                                                            |
| Cell thickness            | All  | float           |   mm  | The thickness of the capillary glass.                                                                                           |
| µ (cell)                  | All  | float           |  1/mm | Mass attenuation coefficient of the cell material.                                                                              |
| µ (sample)                | All  | float           |  1/mm | Mass attenuation coefficient of the sample material.                                                                            |
| Detector/sample distance  | All  | float           |   mm  | The orthogonal distance from probe center to detector.                                                                                   |
| Pixel size (X,Y)          | 1    | float,float     | mm,mm | The size of pixels on the detector (width, height).                                                                             |
| Resolution                | 1    | float,float     | px,px | The number of pixels along each axis of the detector (x,y).                                                                     |
| Offset                    | All  | float,float     | mm,mm | The capillary's offset from the south-east detector corner.                                                                     |
| Ray dimensions (X,Y)      | All  | float,float     | mm,mm | The width and height of the xray beam.                                                                                          |
| Ray offset (X,Y)          | All  | float,float     | mm,mm | The offset of the xray beam from the probe center (x,y).                                                                        |
| Ray profile               | All  | Dropdown        | -     | The shape of the xray beam profile. Currently only rectangle is supported.                                                      |
| Filename of angle list    | 0,2  | string          | -     | The filename (without extension) of a list of angles in `XRD-TAF/Input`.                                                        |
| Ring parameters           | 2    | float,float,int |       | Controls the range of the ring segment, and the number of points along it. In order: Start angle, stop angle, number of points. |
  

#### Settings screen

Contains global settings such as flags and default values. All settings have a description text.


### Folder structure

Inside the XRD-TA folder containing the app, this is the folder/file structure:

    ├── Benchmark 
    │   ├── benchmark.csv
    │   └── config.csv
    ├── Input
    │   ├── angle-list1.txt
    │   ├── angle-list2.txt
    │   ├── angle-list3.txt
    │   └── ...
    ├── Logs
    │   └── mode{mode}_debug.txt
    ├── Output
    │   ├── group
    │   │   ├── preset1
    │   │   │   ├── [mode=<mode>][dim=(<res>,<n>,<m>,<k>)] Output.txt
    │   │   │   └── ...
    │   ├── preset2
    │   │   └── [mode=<mode>][dim=(<res>,<n>,<m>,<k>)] Output.txt
    │   ├── preset3
    │   │   └── [mode=<mode>][dim=(<res>,<n>,<m>,<k>)] Output.txt
    │   └── ...
    ├── Presets
    │   ├── preset1.json
    │   ├── preset2.json
    │   ├── preset3.json
    │   └── ...
    └── Settings
        └── settings.json

All output directories will be created when needed, if not present.

#### Benchmark 

Stores benchmark config and results.

`config.csv` is a user-generated table containing parameters for each iteration of the benchmark.  
`<timestamp> benchmark.csv` contains a table of benchmark results for `config.csv`, generated at the timestamp.

Benchmark parameters are as follows:

`mode` is a value in (0,1,2), which stand for 0=point, 1=area, 2=integrated.  
`res` is the grid resolution of the capillary cross-section.  
`n`: The number of angles to use in the benchmark. In mode 1, `n` this is equivalent to the number of pixels on the horizontal axis of the detector.  
`m`: The number of pixels on the vertical axis of the detector. (Ignored in mode 0 and 2).  
`k`: Amount of angles in the projected ring segment of mode 2. (Ignored in mode 0 and 1).  

#### Input 

The directory where the app searches for angle lists. Lists are expected to have one value per line and no header.

#### Logs

If enabled in the settings, log files will be saved in this folder.
The log filename will be `<timestamp> log.txt`.

#### Output

Calculated absorption factors will be saved in this folder. They will be grouped in folders by preset name -- and if a parent folder is specified in the preset, the whole preset folder will be put in a parent folder see section [Folder structure](### Folder structure).

The filename for each output is of the format `[mode=<mode>][dim=(<res>,<n>,<m>,<k>)] Output.txt`.

#### Presets

The directory where are stored, in the format `<preset name>.json`.
The name specified in the preset is identical to the filename (without extension).

#### Settings

Stores global user settings in `settings.json`.

## Benchmark

### Preset

The pre-defined preset `benchmark.json` will be used for benchmarks. Changes to this preset do not influence the benchmark, since the app takes all time-complexity-relevant parameters from `config.csv`. Angle files will be ignored, since angle lists will be generated from the parameter `n`, i.e. `n` values evenly distributed between 0 and 180°.

### Config

To add a benchmark iteration to the config, add a line containing the parameters `mode`, `res`, `n`, `m` `k` in the exact same order to `Benchmark/config.csv`.

Example:

| mode | res | n    | m   | k  |
|------|-----|------|-----|----|
|0     | 100 | 500  | 0   | 0  |
|0     | 200 | 500  | 0   | 0  |
|1     | 80  | 128  | 128 | 0  |
|1     | 80  | 256  | 256 | 0  |
|2     | 100 | 500  | 0   | 20 |
|2     | 200 | 500  | 0   | 20 |

Parameter `m` will be ignored by modes 0 and 2, `k` will be ignored by mode 0 and 1.

The app expects tabs as value separators for the csv.

## Value format

All float input values, in the GUI and in input files, are expectd in invariant culture style, i.e. with `.` as decimal separator.

All `.csv` files have to use tabs as separators.
