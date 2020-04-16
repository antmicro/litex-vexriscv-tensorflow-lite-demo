[![Build Status](https://travis-ci.com/antmicro/litex-vexriscv-tensorflow-lite-demo.svg?branch=master)](https://travis-ci.com/antmicro/litex-vexriscv-tensorflow-lite-demo)

# Tensorflow lite demo running in Zephyr on Litex/VexRiscv SoC

This repository collects all the repositories required to build and run Zephyr Tensorflow Lite demos on either:

* a Digilent Arty board
* the open source Renode simulation framework (no hardware required)

The hardware version of the `magic wand` demo also requires a [PmodACL](https://store.digilentinc.com/pmod-acl-3-axis-accelerometer/) to be connected to the `JD` port of the Arty board.

## Prerequisites
Install Python and west:
```bash
sudo apt update
sudo apt install python python3-pip python3-setuptools python3-tk python3-wheel
sudo pip3 install west
```

Clone the repository and submodules:
```bash
git clone https://github.com/antmicro/litex-vexriscv-tensorflow-lite-demo
cd litex-vexriscv-tensorflow-lite-demo
DEMO_HOME=`pwd`
west init -m https://github.com/antmicro/zephyr.git --mr tf-lite
git submodule update --init --recursive
```

Install remaining prerequisites (tested on Ubuntu 18.04):
```bash
sudo apt install cmake ninja-build gperf ccache dfu-util device-tree-compiler wget xz-utils file make gcc gcc-multilib locales tar curl unzip
```

Install Zephyr prerequisites:
```bash
# update cmake to required version
sudo pip3 install cmake
# install Zephyr SDK
wget https://github.com/zephyrproject-rtos/sdk-ng/releases/download/v0.10.3/zephyr-sdk-0.10.3-setup.run
chmod +x zephyr-sdk-0.10.3-setup.run
sudo ./zephyr-sdk-0.10.3-setup.run -- -d /opt/zephyr-sdk
sudo pip3 install -r zephyr/scripts/requirements.txt
```

## Building the demos

Setup the environment
```bash
export ZEPHYR_TOOLCHAIN_VARIANT=zephyr
export ZEPHYR_SDK_INSTALL_DIR=/opt/zephyr-sdk
export ZEPHYR_BASE=`pwd`/zephyr
```

### Hello World demo

Build the `Hello World` demo with:
```bash
cd $DEMO_HOME/tensorflow
make -f tensorflow/lite/micro/tools/make/Makefile TARGET=zephyr_vexriscv hello_world_bin
```
The resulting binaries can be found in the `tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/hello_world/CMake/zephyr` folder.

### Magic Wand demo

Build the `Magic Wand` demo with:
```bash
cd $DEMO_HOME/tensorflow
make -f tensorflow/lite/micro/tools/make/Makefile TARGET=zephyr_vexriscv magic_wand_bin
```
The resulting binaries can be found in the `tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/magic_wand/CMake/zephyr` folder.

## Building the gateware

Note: For this section, if you have not already updated your udev rules, follow the instructions at "[Download & setup udev rules](https://github.com/timvideos/litex-buildenv/wiki/HowTo-LCA2018-FPGA-Miniconf#download--setup-udev-rules)" -- you probably won't need to reboot.

The FPGA bitstream (gateware) can be built using [Litex Build Environment](https://github.com/timvideos/litex-buildenv).
Building the gateware currently requires Xilinx's FPGA tooling, Vivado, to be installed in the system.

Note: Some of LiteX BuilEenv's scritps have problems when running in a git repository in detached state.
Execute `git checkout -b tf_demo` in the `litex-buildenv` directory after cloning to avoid build errors.

Build the gateware with:
```bash
cd $DEMO_HOME/litex-buildenv
export CPU=vexriscv
export CPU_VARIANT=full
export PLATFORM=arty
export FIRMWARE=zephyr
export TARGET=tf

./scripts/download-env.sh
source scripts/enter-env.sh

make gateware
```

Once you have synthesized the gateware, load it onto the FPGA with:

```bash
make gateware-load
```

See the [Litex Build Environment Wiki](https://github.com/timvideos/litex-buildenv/wiki/Getting-Started) for more available options.

## Simulating in Renode

The `renode` directory contains a model of the ADXL345 accelerometer and all necessary scripts and assets required to simulate the `Magic Wand` demo.

Build the `Magic Wand` demo as described [in the section above](#magic-wand-demo).

Install Renode as [detailed in its README file](https://github.com/renode/renode/blob/master/README.rst#installation).

Now you should have everything to run the simulation:
```bash
cd $DEMO_HOME/renode
renode -e "s @litex-vexriscv-tflite.resc"
```

You should see the following output on the UART (which will open as a separate terminal in Renode automatically):
```
Got id: 0xe5
***** Booting Zephyr OS build v1.7.99-22021-ga6d97078a3e2 *****
Got accelerometer
RING:
          *
       *     *
     *         *
    *           *
     *         *
       *     *
          *
SLOPE:
        *
       *
      *
     *
    *
   *
  *
 * * * * * * * *
RING:
          *
       *     *
     *         *
    *           *
     *         *
       *     *
          *
SLOPE:
        *
       *
      *
     *
    *
   *
  *
 * * * * * * * *
```

Refer to the [Renode documentation](https://renode.readthedocs.org) for details on how to use Renode to implement more complex usage scenarios, and use its advanced debug capabilities, or set up CI testing your ML-oriented system. If you need commercial support, please contact us at [support@renode.io](mailto:support@renode.io).
