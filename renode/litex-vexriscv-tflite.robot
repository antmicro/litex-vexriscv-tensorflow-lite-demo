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

    Execute Command           include @${CURDIR}/ADXL345.cs
    Execute Command           include @${CURDIR}/LiteX_I2C_Zephyr.cs

    Execute Command           mach create
    Execute Command           machine LoadPlatformDescription @${CURDIR}/litex-vexriscv-tflite.repl

    Execute Command           showAnalyzer uart
    Execute Command           showAnalyzer uart Antmicro.Renode.Analyzers.LoggingUartAnalyzer

    Execute Command           sysbus LoadELF @${CURDIR}/../tensorflow/tensorflow/lite/experimental/micro/examples/magic_wand/zephyr_riscv/build/zephyr/zephyr.elf

    Execute Command           i2c.adxl345 MaxFifoDepth 4
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/circle.data
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/angle.data
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/circle.data
    Execute Command           i2c.adxl345 FeedSample @${CURDIR}/angle.data
    Execute Command           i2c.adxl345 FeedSample 0 0 0 32

    Create Terminal Tester    sysbus.uart  timeout=60

    Start Emulation

    Wait For Line On Uart     Got id: 0xe5
    Wait For Line On Uart     Booting Zephyr OS
    Wait For Line On Uart     Got accelerometer

    Wait For Ring
    Wait For Slope
    Wait For Ring
    Wait For Slope

