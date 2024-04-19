# Microsoft Graph cloud support tool

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

## Overriding API results

### Excluding clouds

The file cloud-exclusions.json contains exclusions for individual APIs -> cloud. Add one entry for each API + cloud that should be excluded.

- `apiPath` is the path from the OpenAPI description.

### Overriding paths

The file overrides.json contains overrides.

- `apiPath` is the path from the docs.
- `overridePath` is the path to map to that shows up in the OpenAPI description.
