# ninaAPI

API for the astronomy imaging software N.I.N.A.
The documentation for the api can be found [here](https://christian-photo.github.io/github-page/projects/ninaAPI/v2/doc/api), and the documentation for the websockets is available [here](https://github.com/christian-photo/ninaAPI/wiki/Websocket-V2).

This plugin aims to be an all-around api for building your own custom tools and apps! Let me know about any cool projects you make, at the bottom of the readme you can find a list containing cool projects using the api!

Many thanks to tcpalmer and his [web session plugin](https://github.com/tcpalmer/nina.plugin.web/tree/main), from which I took a lot of inspiration (and a few code snippets). I want to thank everyone for creating issues, reporting bugs to me and requesting new features! It really helps me a lot while trying to continually improve the api. All that to say, feature requests, bug reports and all of these things are highly welcome!

If you want, you can [buy me a pizza](https://buymeacoffee.com/christian.palm), I would really appreciate it!

### Versioning

The versioning of the api works as follows:

- The first number is the major version, and it indicates the current version of the api. Right now it is `v2/api`. This means, that if a 3.x.x.x version is released, there will be a new url path `v3/api`.
- The second number is the minor version. Here, breaking changes often occur, bigger features are released with an increase in this version number.
- The third number is incremented for smaller feature updates and bug fixes.
- The fourth number is incremented for small 'emergency' bug fixes.

### Acknowledgements

- This project uses [EmbedIO](https://github.com/unosquare/embedio) for its Webserver
- [NetVips](https://github.com/kleisauke/net-vips) is used to support a variety of different image formats

---

Take a look at these projects:

- [Touch'N'Stars: WebApp for Mobile Control of NINA](https://github.com/Touch-N-Stars/Touch-N-Stars)
- [N.I.N.A Model Context Protocol Server for Advanced API Plugin v2 (MCP)](https://github.com/PaDev1/Nina_advanced_api_mcp)
- [Mobile interface to Manage NINA using the Advanced API plugin](https://github.com/venturachrisdev/Cygnus-Astro)
- [Your Personal Astrophotography Catalog - CosmosCollection](https://github.com/quake101/CosmosCollection)
