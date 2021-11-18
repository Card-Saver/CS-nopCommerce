Disclaimer: Please note that we no longer support older versions of SDKs and Modules. We recommend that the latest versions are used.

# P3 Payment Gateway for NopCommerce

**Compatibility**

**Compatible with NopCommerce 4.20 and up**

Supports Hosted integration

## Installation
**Step 1:** Log In to Admin Panel and from menu Configurations -> Local Plugins

**Step 2:** Find `Upload plugin or theme` button and upload `Payments.P3Gateway.zip` file, 

**Step 3:** Find on `Configuration → Payment methods` then click on the `Edit`  next to the `Payments.P3Gateway` and select the `Is active` checkbox and finally `Update`.

**Step 4:** Configure the plugin settings by clicking the `Configure` button

## Manual Installation
**Step 1:** upload to build environment the contents from `httpdocs` folder, 
build the plugin project and then publish the whole solution/project 

**Note:** following the `Dockerfile` used for build by platform team this will be:
https://github.com/nopSolutions/nopCommerce/blob/develop/Dockerfile

    ...
    WORKDIR /src/Plugins/Nop.Plugin.Widgets.NivoSlider
    RUN dotnet build Nop.Plugin.Widgets.NivoSlider.csproj -c Release

    # build plugin
    WORKDIR /src/Plugins/Nop.Plugin.Payments.P3Gateway
    RUN dotnet build Nop.Plugin.Payments.P3Gateway.csproj -c Release

    # publish project
    WORKDIR /src/Presentation/Nop.Web   
    RUN dotnet publish Nop.Web.csproj -c Release -o /app/published    
    ...

**Step 1:** Upload zip file to 

**Step 2:** Click on `Configuration → Local plugins` then click on the `Install` link to install the plugin.

**Step 3:** Go to `Configuration → Local plugins`. Enable the plugin by clicking the `Edit` button
and checking the `Is enabled` checkbox.

**Step 4:** Configure the plugin settings by clicking the `Configure` button

## FAQ
**More info about plugins in NopCommerce.**
https://docs.nopcommerce.com/en/getting-started/advanced-configuration/plugins-in-nopcommerce.html


