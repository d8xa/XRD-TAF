# XRD-TAF

*X Ray Diffraction - Theoretical Absorption Factors*

A GUI app to compute theoretical xray absorption factors (for xray diffracted in a capillary) efficiently on the device's GPU. Made with C#/Unity.

## Installation

Extract the zip file and put the folder wherever you want to run the app.

## How to use

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

All directories will be created on output, if not present.

#### Benchmark 

Stores benchmark config and results.

`config.csv` is a user-generated table containing parameters for each iteration of the benchmark.  
`benchmark.csv` contains a table of benchmark results for `config.csv`. The results will be overwritten on benchmark.


#### Input 

The directory where the app searches for angle lists. Lists are expected to have one value per line and no header.

#### Logs

If enabled in the settings, log files will be saved in this folder.
The log filename will be `mode<mode>_debug.txt`. Will be overwritten on each execution.

#### Output

Calculated absorption factors will be saved in this folder. They will be grouped in folders by preset name -- and if a parent folder is specified in the preset, the whole preset folder will be put in a parent folder see section [Folder structure](### Folder structure).

The filename for each output is of the format `[mode=<mode>][dim=(<res>,<n>,<m>,<k>)] Output.txt`.

#### Presets

The directory where user-defined presets will be save, in the format `<preset name>.json`.
The name specified in the preset is identical to the filename (without extension).

#### Settings

Stores global user settings in `setting.json`.

## Benchmark

### Preset

The pre-defined preset `benchmark.json` will be used for benchmarks. Changes to this preset do not influence the benchmark, since `config.csv` contains all time-complexity-relevant parameters.

### Config

Configurable parameters to the benchmark are `mode`, `res`, `n`, `m`, `k`. 

`mode` is a value in (0,1,2), which stand for 0=point, 1=area, 2=integrated.  
`res` is the grid resolution of the capillary cross-section.  
`n`: The number of theta angles to use in the benchmark. In mode 1, `n` this is equivalent to the number of pixels on the horizontal axis of the detector.  
`m`: The number of pixels on the vertical axis of the detector. (Ignored in mode 0 and 2).  
`k`: Amount of angles in the projected ring segment of mode 2. (Ignored in mode 0 and 1).  

To add a benchmark iteration to a config, add a line containing these parameters in the exact same order to `Benchmark/config.csv`.

Example:

| mode | res | n    | m   | k  |
|------|-----|------|-----|----|
| 0    | 60  | 1000 | 0   | 0  |
| 0    | 200 | 1000 | 0   | 0  |
| 1    | 60  | 256  | 256 | 0  |
| 2    | 200 | 1000 | 0   | 30 |


The app expects a tab as separator for the csv.

## Value format

All float input values, in the GUI and in input files, are expectd in invariant culture style, i.e. with `.` as decimal separator.

All `.csv` files have to use tabs as separators.
