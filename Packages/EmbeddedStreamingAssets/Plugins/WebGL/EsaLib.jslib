mergeInto(LibraryManager.library, {
    EsaLib_Init: function (fnPtr) {
        const EsaLib = {
            result: ""
        };

        Module.EsaLib = EsaLib;

        const originalFetch = window.fetch;
        const origin = window.location.origin;
        const prefixAbs = origin + '/StreamingAssets/';

        window.fetch = function (url, options) {
            if (!url.startsWith(prefixAbs)) {
                return originalFetch(url, options);
            }
            try {
                var relativeUrl = url.replace(prefixAbs, ``);
                console.log(`Intercepted fetch for StreamingAssets:`, relativeUrl);
                const bufferSize = lengthBytesUTF8(relativeUrl) + 1
                const buffer = _malloc(bufferSize)
                stringToUTF8(relativeUrl, buffer, bufferSize)

                try {
                    Module.dynCall_vi(fnPtr, [buffer]);

                } finally {
                    _free(buffer);
                }

                if (EsaLib.result) {
                    const data = EsaLib.result;
                    delete EsaLib.result;
                    if (typeof data === 'string') {
                        return Promise.reject(new Error(data));
                    }
                    if (typeof data !== 'undefined') {
                        const blob = new Blob([data]);
                        return Promise.resolve(new Response(blob, {status: 200}));
                    }
                }
                return Promise.reject(new Error(`No data returned from EsaLib`));
            } catch (e) {
                console.error(e);
                return Promise.reject(e);
            }
        };
    }
    ,

    EsaLib_Resolve: function (dataPtr, length) {
        try {
            var src = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);
            var copy = new Uint8Array(length);
            copy.set(src);
            Module.EsaLib.result = copy
        } catch (e) {
            console.error('EsaLib_Resolve error', e);
        }
    }
    ,

    EsaLib_Reject: function (messagePtr) {
        try {
            var message = UTF8ToString(messagePtr);
            Module.EsaLib.result = message;
        } catch (e) {
            console.error('EsaLib_Reject error', e);
        }
    }
})
;
