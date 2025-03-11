mergeInto(LibraryManager.library, {
	_open_popup: function(name, url, redirectUri) {
		var stringify = (UTF8ToString === undefined) ? Pointer_stringify : UTF8ToString;
		var popupUrl = stringify(url);
		var nameStr = stringify(name);
		var redirectUriStr = stringify(redirectUri);
        const ecosystemWalletDomain = 'https://id.sample.openfort.xyz';

		console.log("Opening popup with URL: " + ecosystemWalletDomain);
		console.log("Redirect URI: " + redirectUriStr);
		
		// Parse parameters from the URL
		function parseUrlParams(url) {
			var params = {};
			try {
				// Create URL object to easily extract search params
				var urlObj = new URL(url);
				var searchParams = new URLSearchParams(urlObj.search);
				
				// Convert search params to object
				for (var pair of searchParams.entries()) {
					var key = pair[0];
					var value = pair[1];
					
					// Try to parse JSON strings (handle quoted values)
					try {
						// If value is a quoted string like "\"value\"", parse it to remove quotes
						if (value.startsWith('"') && value.endsWith('"')) {
							value = JSON.parse(value);
						}
						
						// If it's a JSON object or array string, parse it
						if ((value.startsWith('{') && value.endsWith('}')) || 
							(value.startsWith('[') && value.endsWith(']'))) {
							value = JSON.parse(value);
						}
					} catch (e) {
						// Keep as is if parsing fails
						console.log("Could not parse value for " + key + ", using as is");
					}
					
					params[key] = value;
				}
				
				// Add any hash params if present
				if (urlObj.hash && urlObj.hash.length > 1) {
					var hashParams = new URLSearchParams(urlObj.hash.substring(1));
					for (var pair of hashParams.entries()) {
						var key = pair[0];
						var value = pair[1];
						
						// Try to parse JSON strings
						try {
							// If value is a quoted string
							if (value.startsWith('"') && value.endsWith('"')) {
								value = JSON.parse(value);
							}
							
							// If it's a JSON object or array string
							if ((value.startsWith('{') && value.endsWith('}')) || 
								(value.startsWith('[') && value.endsWith(']'))) {
								value = JSON.parse(value);
							}
						} catch (e) {
							// Keep as is if parsing fails
						}
						
						params[key] = value;
					}
				}
			} catch (e) {
				console.error("Error parsing URL: ", e);
			}
			return params;
		}
		
		// Helper function to convert base64 to ArrayBuffer
		function base64ToArrayBuffer(base64) {
			var binaryString = window.atob(base64);
			var len = binaryString.length;
			var bytes = new Uint8Array(len);
			for (var i = 0; i < len; i++) {
				bytes[i] = binaryString.charCodeAt(i);
			}
			return bytes.buffer;
		}
		
		// Helper function to convert ArrayBuffer to base64
		function arrayBufferToBase64(buffer) {
			var binary = '';
			var bytes = new Uint8Array(buffer);
			var len = bytes.byteLength;
			for (var i = 0; i < len; i++) {
				binary += String.fromCharCode(bytes[i]);
			}
			return window.btoa(binary);
		}
		
		// Convert response object back to URL params
		function responseToUrlParams(response) {
			var urlParams = new URLSearchParams();
			
			// Add basic response properties
			if (response.id) urlParams.append('id', JSON.stringify(response.id));
			if (response.requestId) urlParams.append('requestId', JSON.stringify(response.requestId));
			if (response.sender) urlParams.append('sender', JSON.stringify(response.sender));
			if (response.timestamp) urlParams.append('timestamp', JSON.stringify(new Date(response.timestamp).toISOString()));
			
			// Handle content based on its structure
			if (response.content) {
				if (response.content.failure) {
					urlParams.append('errorCode', response.content.failure.code);
					urlParams.append('errorMessage', response.content.failure.message);
					if (response.content.failure.data) {
						urlParams.append('errorData', JSON.stringify(response.content.failure.data));
					}
				} else if (response.content.encrypted) {
					// For encrypted content, we need to convert ArrayBuffers to base64
					var ivBase64 = arrayBufferToBase64(response.content.encrypted.iv);
					var cipherTextBase64 = arrayBufferToBase64(response.content.encrypted.cipherText);
					var content = {
						encrypted: {
							iv: ivBase64,
							cipherText: cipherTextBase64
						}
					};
					urlParams.append('content', JSON.stringify(content));
				}
			}
			
			return redirectUriStr + '?' + urlParams.toString();
		}
		
        // Open popup window
        var width = 420, height = 540;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        
        var popupWindow = window.open(ecosystemWalletDomain, "Ecosystem Wallet",
            `width=${width}, height=${height}, top=${top}, left=${left}`);

        if (!popupWindow) {
            console.error("Popup blocked!");
            SendMessage(nameStr, "OnMessage", 'CallOnError:Popup was blocked by browser');
            return;
        }
        
        // Extract parameters from URL
        var urlParams = parseUrlParams(popupUrl);
        console.log("Parsed URL parameters:", urlParams);
        // Check for required parameters
        if (!urlParams.id || !urlParams.sender) {
            console.error("Missing required parameters (id or sender) in URL");
            SendMessage(nameStr, "OnMessage", 'CallOnError:Missing required parameters in URL');
            if (popupWindow && !popupWindow.closed) {
                popupWindow.close();
            }
            return;
        }
        
        // Create the rpc-request message using values from URL
        var request = {
            topic: 'rpc-requests',
            id: urlParams.id,
            requestId: urlParams.requestId || null,
            sender: urlParams.sender,
            timestamp: urlParams.timestamp || new Date(),
            response: undefined
        };
        
        // Determine content type based on content parameter or URL parameters
        if (urlParams.content.encrypted) {
            // Use encrypted content type
            request.content = {
                encrypted: {
                    iv: base64ToArrayBuffer(urlParams.content.encrypted.iv),
                    cipherText: base64ToArrayBuffer(urlParams.content.encrypted.cipherText)
                }
            };
        } else {
            // Use handshake content type with clean params (remove special fields)
            request.content = urlParams.content
        }
        console.log("Created RPC request:", request);
        
        // Track if we've received a response
        var responseReceived = false;
        var isInitialized = false;
        var readyTimeoutId = null;
        
        // Function to handle all incoming messages
        function handleMessages(event) {
            try {
                var data = event.data;
                
                // If data is a string, try to parse it as JSON
                if (typeof data === 'string') {
                    try {
                        data = JSON.parse(data);
                    } catch (e) {
                        console.warn("Received non-JSON string message, ignoring");
                        return;
                    }
                }
                
                // Handle different message topics
                if (data && data.topic === 'ready' && !isInitialized) {
                    console.log("Received 'ready' message from popup:", data);
                    isInitialized = true;
                    
                    // Clear ready timeout if it exists
                    if (readyTimeoutId) {
                        clearTimeout(readyTimeoutId);
                        readyTimeoutId = null;
                    }
                    
                    // Send initialization data
                    popupWindow.postMessage({
                        requestId: data.id,
                        content: {
                            type: 'init',
                            version: '1.0.0', // Use appropriate version
                            mode: 'popup',
                            referrer: {
                                origin: location.origin,
                                title: document.title,
                                icon: undefined // Add app logo URL if available
                            }
                        },
                        topic: '__internal',
                        id: crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).substring(2),
                        timestamp: new Date(),
                        response: undefined
                    }, ecosystemWalletDomain);
                    
                    // Now that we're initialized, send the actual RPC request
                    console.log("Sending RPC request to popup:", request);
                    popupWindow.postMessage(request, ecosystemWalletDomain);
                    
                } else if (data && data.topic === 'rpc-response') {
                    // Handle RPC response
                    console.log("Received RPC response from popup:", data, request);
                    
                    // Check if this response matches our request
                    if (data.requestId && data.requestId === request.id) {
                        responseReceived = true;
                        
                        // Close the popup window
                        if (popupWindow && !popupWindow.closed) {
                            popupWindow.close();
                        }
                        
                        // Convert the response to URL params and send to Unity
                        var responseUrl = responseToUrlParams(data);
                        SendMessage(nameStr, "OnMessage", "CallFromAuthCallback:"+responseUrl);
                        
                        // Clear the monitoring interval
                        if (monitorInterval) {
                            clearInterval(monitorInterval);
                        }
                    }
                } else if (data && data.topic === 'close') {
                    // Handle close message
                    console.log("Received 'close' message from popup");
                    
                    // Remove the event listener
                    window.removeEventListener('message', handleMessages);
                    
                    // Close the popup window if it's still open
                    if (popupWindow && !popupWindow.closed) {
                        popupWindow.close();
                    }
                    
                    // Clear the monitoring interval
                    if (monitorInterval) {
                        clearInterval(monitorInterval);
                    }
                }
            } catch (e) {
                console.error("Error handling message:", e);
            }
        }
        
        // Add event listener for all postMessage events
        window.addEventListener('message', handleMessages);
        
        // Set a timeout for the ready message
        readyTimeoutId = setTimeout(function() {
            if (!isInitialized) {
                console.error("Timeout: No 'ready' message received within 3 seconds");
                window.removeEventListener('message', handleMessages);
                
                if (popupWindow && !popupWindow.closed) {
                    popupWindow.close();
                }

                SendMessage(nameStr, "OnMessage", 'CallOnError:Timeout waiting for ready message');
            }
        }, 3000); // 3-second timeout
        
        // Monitor popup window status
        var monitorInterval = setInterval(function() {
            if (popupWindow && popupWindow.closed && !responseReceived) {
                console.log("Popup was closed without a response");
                clearInterval(monitorInterval);
                window.removeEventListener('message', handleMessages);
                SendMessage(nameStr, "OnMessage", 'CallOnError:User closed the popup');
            }
        }, 500);
    }
});