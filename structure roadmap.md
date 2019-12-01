```
.
└── fopra_absorb  
    ├── [main class ?]  
    ├── util  
    │   ├── parser  
    │   └── [...]  
    ├── [gui]  
    │   └── [...]  
    └── math  
        ├── [class]  
        └── [...]  
```

"`[]`" stands for not yet existing.

* `[main class]`: loads all modules and classes it needs and starts the GUI, if existing.  
* `[gui]`: a standalone module, to develop the GUI independently from the CLI, if possible.  
* `parser`: parses commandline arguments and formats as input for math entrypoint in `[main class]`.    
* `math`: contains all calc. functions and helpers for calc.  
* `util`: for all helper functions/classes. 
