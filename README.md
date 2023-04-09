# rebackground

Replace the background of (almost) any image.

If anyone wants to experiment and include a better image editing model that would be really cool; DALL-E, as good as it is, isn't fantastic with real images and creates some fairly trippy results:

```sh
rebackground -i 'craig.jpg' -o 'craig_rainforest.png' -p 'Broadcasting from a rainforest'
```

![Craig](/.github/img/craig.jpg)
![Craig Rainforest](/.github/img/craig_rainforest.png)

## Setup

1. Clone the repo

   ```sh
   git clone git@github.com:king-millez/rebackground.git
   ```

   ```sh
   cd rebackground
   ```

2. Set the remove.bg and OpenAI environment variables.
   - To learn how to set environment variables on your OS, follow [this guide](https://www3.ntu.edu.sg/home/ehchua/programming/howto/Environment_Variables.html). You may find better Windows-specific info [here](https://docs.oracle.com/en/database/oracle/machine-learning/oml4r/1.5.1/oread/creating-and-modifying-environment-variables-on-windows.html).
   - Set `REMOVEBG_API_KEY` to the key obtained from following [this guide](https://www.remove.bg/api#remove-background) (click the big "Get API Key" button).
   - Set `OPENAI_API_KEY` to the key obtained from [this page](https://platform.openai.com/account/api-keys). You need an OpenAI account, etc etc.
   - **Note: you may need to log out and log back in for environment variable changes (especially on Windows) to work.**
3. You can build the application or run it with `dotnet`. This app should work on Windows, MacOS, and Linux. If it doesn't, let me know and I'll fix it ([open a GitHub issue](https://github.com/king-millez/rebackground/issues/new), do not email me).

## Usage

If you don't want to compile, just replace `rebackground` with `dotnet run --`

**You'll need to crop your input images to have a 1:1 ratio, e.g `512x512`, `1080x1080`, etc.**

```sh
rebackground -i <input image> -o <output image (will be a PNG)> -p <DALL-E edit prompt>
```
