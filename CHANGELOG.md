# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2020-12-03
### Fixed
- Fixed bug causing shader in Point mode to use buffer data from previous runs.
- Fixed target material reporting in output preamble when using option "use scalar arrays".
### Changed
- Removed status message and replaced with execution start time and elapsed time.
### Added
- Added a toggle in settings to enable/disable output preamble.

## [1.0.2] - 2020-12-07
### Fixed
- Removed benchmark's dependency on angle files entirely. 

## [1.0.1] - 2020-12-06
### Changed
- Logs and benchmarks now have timestamps prefixed to the filename.
- Updated README.
### Fixed
- Removed dropdown option "testing" from release.
- Now using correct ray offset label in preamble of output files.
- Fixed bug causing Area mode to fail if the angle file specified in the preset did not exist.
- Fixed log writing logic not using the settings toggle.

## [1.0.0] - 2020-11-24
### Added
Initial release.