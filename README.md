[![Build Status](https://travis-ci.com/antmicro/litex-vexriscv-tensorflow-lite-demo.svg?branch=master)](https://travis-ci.com/antmicro/litex-vexriscv-tensorflow-lite-demo)

# Tensorflow lite demo running in Zephyr on Litex/VexRiscv SoC

This repository collects all the repositories required to build and run [Zephyr](https://www.zephyrproject.org/) [TensorFlow Lite Micro](https://www.tensorflow.org/lite/microcontrollers) demos on either:

* a [Digilent Arty A7](https://reference.digilentinc.com/reference/programmable-logic/arty-a7/start) board
* the open source [Renode simulation framework](http://renode.io/) (no hardware required)

The hardware version of the `magic wand` demo also requires a [PmodACL](https://store.digilentinc.com/pmod-acl-3-axis-accelerometer/) to be connected to the `JD` port of the Arty A7 board.

## Prerequisites

Clone the repository and submodules:
```bash
git clone https://github.com/antmicro/litex-vexriscv-tensorflow-lite-demo
cd litex-vexriscv-tensorflow-lite-demo
DEMO_HOME=`pwd`
git submodule update --init --recursive
```

Install prerequisites (tested on Ubuntu 18.04):
```bash
sudo apt update
sudo apt install cmake ninja-build gperf ccache dfu-util device-tree-compiler wget python python3-pip python3-setuptools python3-tk python3-wheel xz-utils file make gcc gcc-multilib locales tar curl unzip xxd
```

Install Zephyr prerequisites:
```bash
# update cmake to required version
sudo pip3 install cmake virtualenv
# install Zephyr SDK
wget https://github.com/zephyrproject-rtos/sdk-ng/releases/download/v0.11.2/zephyr-sdk-0.11.2-setup.run
chmod +x zephyr-sdk-0.11.2-setup.run
sudo ./zephyr-sdk-0.11.2-setup.run -- -d /opt/zephyr-sdk
```

Export Zephyr configuration:
```bash
export ZEPHYR_TOOLCHAIN_VARIANT=zephyr
export ZEPHYR_SDK_INSTALL_DIR=/opt/zephyr-sdk
```

## Building the demos

### Hello World demo

Build the `Hello World` demo with:
```bash
cd $DEMO_HOME/tensorflow
make -f tensorflow/lite/micro/tools/make/Makefile TARGET=zephyr_vexriscv hello_world_bin
```
The resulting binaries can be found in the `tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/hello_world/build/zephyr` folder.

### Magic Wand demo

Build the `Magic Wand` demo with:
```bash
cd $DEMO_HOME/tensorflow
make -f tensorflow/lite/micro/tools/make/Makefile TARGET=zephyr_vexriscv magic_wand_bin
```
The resulting binaries can be found in the `tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/magic_wand/build/zephyr` folder.

## Building the gateware

Note: For this section, if you have not already updated your udev rules, follow the instructions at "[Download & setup udev rules](https://github.com/timvideos/litex-buildenv/wiki/HowTo-LCA2018-FPGA-Miniconf#download--setup-udev-rules)" -- you probably won't need to reboot.

The FPGA bitstream (gateware) can be built using [Litex Build Environment](https://github.com/timvideos/litex-buildenv).
Building the gateware currently requires Xilinx's FPGA tooling, Vivado, to be installed in the system.

Build the gateware with:
```bash
cd $DEMO_HOME/litex-buildenv

# Some of LiteX Buildenv's scritps have problems when running in a git repository in detached state.
# Let's create a fake branch to avoid build errors.
git checkout -b tf_demo

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

With the FPGA programmed, you can load the Zephyr binary on the device using the flterm program provided inside the environment you just initialized above:

```bash
cd $DEMO_HOME/tensorflow
flterm --port=/dev/ttyUSB1 --kernel=tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/magic_wand/build/zephyr/zephyr.bin --speed=115200
```

See the [Litex Build Environment Wiki](https://github.com/timvideos/litex-buildenv/wiki/Getting-Started) for more available options.

## Simulating in Renode

The `renode` directory contains all necessary scripts and assets required to simulate the `Magic Wand` demo.

Install Renode as [detailed in its README file](https://github.com/renode/renode/blob/master/README.rst#installation).

Build the `Magic Wand` demo as described [in the section above](#magic-wand-demo) or use a prebuilt binary from the `binares/magic_wand` directory.

Now you should have everything to run the simulation using the locally built binary:
```bash
cd $DEMO_HOME/renode
renode -e "s @litex-vexriscv-tflite.resc"
```

Or using the prebuilt one: 
```bash
cd $DEMO_HOME/renode
renode -e "set zephyr_elf @../binaries/magic_wand/zephyr.elf; s @litex-vexriscv-tflite.resc"
```

You should see the following output on the UART (which will open as a separate terminal in Renode automatically):
```
*** Booting Zephyr OS build 0.6.0-86741-g626bb2c4d0bd  ***
4 bytes lost due to alignment. To avoid this loss, please make sure the tensor_arena is 16 bytes aligned.
Got accelerometer, label: accel-0

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

In order to exit Renode, type `quit` in Monitor (the window with the Renode logo and `(machine-0)` prompt).

Refer to the [Renode documentation](https://renode.readthedocs.org) for details on how to use Renode to implement more complex usage scenarios, and use its advanced debug capabilities, or set up CI testing your ML-oriented system. If you need commercial support, please contact us at [support@renode.io](mailto:support@renode.io).
