*** Settings ***
Suite Setup                   Setup
Suite Teardown                Teardown
Test Setup                    Reset Emulation
Resource                      ${RENODEKEYWORDS}

*** Keywords ***
Wait For Ring
    Wait For Line On Uart     RING:
    # Passing whitespaces in arguments is a bit tricky.
    # Here we wait for the following pattern:
    #           *
    #        *     *
    #      *         *
    #     *           *
    #      *         *
    #        *     *
    #           *
    Wait For Line On Uart     ${SPACE*10}*
    Wait For Line On Uart     ${SPACE*7}*${SPACE*5}*
    Wait For Line On Uart     ${SPACE*5}*${SPACE*9}*
    Wait For Line On Uart     ${SPACE*4}*${SPACE*11}*
    Wait For Line On Uart     ${SPACE*5}*${SPACE*9}*
    Wait For Line On Uart     ${SPACE*7}*${SPACE*5}*
    Wait For Line On Uart     ${SPACE*10}*

Wait For Slope
    Wait For Line On Uart     SLOPE:
    # Passing whitespaces in arguments is a bit tricky.
    # Here we wait for the following pattern:
    #         *
    #        *
    #       *
    #      *
    #     *
    #    *
    #   *
    #  * * * * * * * *
    Wait For Line On Uart    ${SPACE*8}*
    Wait For Line On Uart    ${SPACE*7}*
    Wait For Line On Uart    ${SPACE*6}*
    Wait For Line On Uart    ${SPACE*5}*
    Wait For Line On Uart    ${SPACE*4}*
    Wait For Line On Uart    ${SPACE*3}*
    Wait For Line On Uart    ${SPACE*2}*
    Wait For Line On Uart    ${SPACE}* * * * * * * *

*** Test Cases ***
Run TF Demo
    Execute Command           using sysbus

    Execute Command           mach create
    Execute Command           machine LoadPlatformDescription @${CURDIR}/litex-vexriscv-tflite.repl

    Execute Command           showAnalyzer uart Antmicro.Renode.Analyzers.LoggingUartAnalyzer

    Execute Command           sysbus LoadELF @${CURDIR}/../tensorflow/tensorflow/lite/micro/tools/make/gen/zephyr_vexriscv_x86_64/magic_wand/build/zephyr/zephyr.elf

    Execute Command           i2c.adxl345 MaxFifoDepth 1
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/circle.data
    Execute Command           i2c.adxl345 FeedSample 0 15000 15000 128
    Execute Command           i2c.adxl345 FeedSample 0 0 0 128
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/angle.data
    Execute Command           i2c.adxl345 FeedSample 0 15000 15000 128
    Execute Command           i2c.adxl345 FeedSample 0 0 0 128
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/circle.data
    Execute Command           i2c.adxl345 FeedSample 0 15000 15000 128
    Execute Command           i2c.adxl345 FeedSample 0 0 0 128
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/angle.data
    Execute Command           i2c.adxl345 FeedSample 0 15000 15000 128
    Execute Command           i2c.adxl345 FeedSample 0 0 0 128

    Create Terminal Tester    sysbus.uart  timeout=480

    Start Emulation

    Wait For Line On Uart     Booting Zephyr OS
    Wait For Line On Uart     Got accelerometer

    Wait For Ring
    Wait For Slope
    Wait For Ring
    Wait For Slope

