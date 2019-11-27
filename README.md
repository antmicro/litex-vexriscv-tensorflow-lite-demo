# Tensorflow lite demo running in Zephyr on Litex/VexRiscv SoC

This repository collects all the repositories required to build and run Zephyr Tensorflow lite demos on Arty board.
`magic wand` demo requires [PmodACL](https://store.digilentinc.com/pmod-acl-3-axis-accelerometer/) to be connected to the `JD` port of the Arty board.

## Prerequisites

Clone the repository and submodules:
```bash
git clone https://github.com/antmicro/litex-vexriscv-tensorflow-lite-demo
cd litex-vexriscv-tensorflow-lite-demo
git submodule update --init --recursive
```

Install prerequisites (tested on Ubuntu 18.04):
```bash
sudo apt update
sudo apt install cmake ninja-build gperf ccache dfu-util device-tree-compiler wget python python3-pip python3-setuptools python3-tk python3-wheel xz-utils file make gcc gcc-multilib locales tar curl unzip
```

Install Zephyr prerequisites:
```bash
sudo pip3 install west
# update cmake to required version
sudo pip3 install cmake
# install Zephyr SDK
wget https://github.com/zephyrproject-rtos/sdk-ng/releases/download/v0.10.3/zephyr-sdk-0.10.3-setup.run
chmod +x zephyr-sdk-0.10.3-setup.run
./zephyr-sdk-0.10.3-setup.run -- -d /opt/zephyr-sdk
sudo pip3 install -r zephyr/scripts/requirements.txt
```

## Building the demos

Setup the environment
```bash
export ZEPHYR_TOOLCHAIN_VARIANT=zephyr
export ZEPHYR_SDK_INSTALL_DIR=/opt/zephyr-sdk
export ZEPHYR_BASE=`pwd`/zephyr
```

Download Tensorflow third party dependencies
```bash
cd tensorflow
make -f tensorflow/lite/experimental/micro/tools/make/Makefile third_party_downloads
```

### Hello World demo

Build the `Hello World` demo with:
```bash
cd tensorflow/tensorflow/lite/experimental/micro/examples/hello_world/zephyr_riscv
make build_zephyr_litex -j`nproc`
```
The resulting binaries can be found in the `build/zephyr` folder.

### Magic Wand demo

Build the `Magic Wand` demo with:
```bash
cd tensorflow/tensorflow/lite/experimental/micro/examples/magic_wand/zephyr_riscv
make build_zephyr_litex -j`nproc`
```
The resulting binaries can be found in the `build/zephyr` folder.

## Building the gateware

Gateware FPGA bitstream can be build using [Litex Build Environment](https://github.com/timvideos/litex-buildenv).
Building gateware requires Vivado to be installed in the system.

Build the gateware with:
```bash
cd litex-buildenv
export CPU=vexriscv
export CPU_VARIANT=full
export PLATFORM=arty
export FIRMWARE=zephyr
export TARGET=tf

./scripts/download-env.sh
source scripts/enter-env.sh

make gateware
```

Once you have a synthesized gateware, load it onto the FPGA with:
```bash
make gateware-load
```

See [Litex Build Environment Wiki](https://github.com/timvideos/litex-buildenv/wiki/Getting-Started) for more available options.
