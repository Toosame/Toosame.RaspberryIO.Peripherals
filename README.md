# RaspberryIO Peripherals

Support device:

+ TSL2561

+ PCA9685 for WaveShare Pan-Tilt-HAT

## How to use

### TSL2561

```csharp
TSL2561 tsl2561 = new TSL2561();

Console.WriteLine("TSL2581 ID: {0}", tsl2561.ReadId() & 0xf0);

while (!cancellationTokenSource.Token.IsCancellationRequested)
{
    Console.WriteLine("Lux: {0}", tsl2561.GetLux(2, 148));

    await Task.Delay(1000);
}
```

### PCA9685 for WaveShare Pan-Tilt-HAT
```csharp
PCA9685 pca9685 = new PCA9685();

const int step = 5;//5°
int rightToLeft = 95;
int upToDown = 120;

//init position
pca9685.SetRotationAngle(0, rightToLeft);
pca9685.SetRotationAngle(1, upToDown);

while (!cancellationTokenSource.IsCancellationRequested)
{
    var key = Console.ReadKey().Key;
    if (key == ConsoleKey.UpArrow)
    {
        int upToDownAfter = upToDown - step;
        if (upToDownAfter <= 180 && upToDownAfter >= 70)
        {
            pca9685.SetRotationAngle(1, upToDownAfter);
            upToDown = upToDownAfter;
        }
    }
    else if (key == ConsoleKey.DownArrow)
    {
        int upToDownAfter = upToDown + step;
        if (upToDownAfter <= 180 && upToDownAfter >= 70)
        {
            pca9685.SetRotationAngle(1, upToDownAfter);
            upToDown = upToDownAfter;
        }
    }
    else if (key == ConsoleKey.RightArrow)
    {
        int rightToLeftAfter = rightToLeft + step;
        if (rightToLeftAfter <= 180 && rightToLeftAfter >= 0)
        {
            pca9685.SetRotationAngle(0, rightToLeft);
            rightToLeft = rightToLeftAfter;
        }
    }
    else if (key == ConsoleKey.LeftArrow)
    {
        int rightToLeftAfter = rightToLeft - step;
        if (rightToLeftAfter <= 180 && rightToLeftAfter >= 0)
        {
            pca9685.SetRotationAngle(0, rightToLeft);
            rightToLeft = rightToLeftAfter;
        }
    }

    Console.WriteLine("upToDown:" + upToDown);
    Console.WriteLine("rightToLeft:" + rightToLeft);

    await Task.Delay(20);
}
```