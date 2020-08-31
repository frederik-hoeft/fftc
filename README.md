# fftc
The Fast File Transfer Client is a simple, light weight command line utility to transfer files in the local area network.

This utility was created as a fast, simple, cross platform alternative to netcat for local file sharing as `ncat` can be quite slow on Windows at times.

---

### Installation

1. Go to the [Releases page](https://github.com/frederik-hoeft/fftc/releases) and download the latest binaries for your system.
2. Unzip the binaries using your favourite tool.
3. Rename the unzipped directory to `fftc/`
4. Add the `fftc/` directory to your system path, so you will be able to execute `fftc` from anywhere on your system.
5. (Linux and OSX only) You may need to make the binary executable using `chmod +x fftc`

---

### Usage

#### Send a file to a specified ip and port:
```
fftc [-c|-s] <ip> <port> -f <filename>
```
##### Example:
Send a file called _myfile.zip_ to `10.1.1.12`, port `12344`:
```
fftc -s 10.1.1.12 12344 -f myfile.zip
```
#### Listen for a file at a specified port:
```
fftc -l <port> -f <filename>
```
##### Example:
Listen for a file called _myfile.zip_ on port `12344`:
```
fftc -l 12344 -f myfile.zip
```
