/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 *
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

#import <AppKit/AppKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

extern "C" typedef void (*DelegateCallbackFunction)(const char * key, const char * message);

DelegateCallbackFunction delegateCallback = NULL;

@interface CWebViewPlugin : NSObject<ASWebAuthenticationPresentationContextProviding>
{
}
@end

@implementation CWebViewPlugin

static NSMutableArray *_instances = [[NSMutableArray alloc] init];
static ASWebAuthenticationSession *_authSession;

- (id)initWithUa:(const char *)ua
{
    self = [super init];
    return self;
}

- (void)dispose
{
    delegateCallback = nil;
}

- (void)sendUnityCallback:(const char *)key message:(const char *)message {
    if (delegateCallback != nil) {
        delegateCallback(key, message);
    } else {
        NSLog(@"delegateCallback is nil, message not sent.");
    }
}

- (void)launchAuthURL:(const char *)url redirectUri:(const char *)redirectUri
{
    NSURL *URL = [[NSURL alloc] initWithString: [NSString stringWithUTF8String:url]];
    // Bundle identifier does not work like iOS, so using callback URL scheme
    // from redirect URI instead
    NSString *redirectUriString = [[NSString alloc] initWithUTF8String:redirectUri];
    NSString *callbackURLScheme = [[redirectUriString componentsSeparatedByString:@":"] objectAtIndex:0];

    _authSession = [[ASWebAuthenticationSession alloc] initWithURL:URL callbackURLScheme:callbackURLScheme completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error) {
        _authSession = nil;

        if (error != nil && (error.code == 1 || error.code == ASWebAuthenticationSessionErrorCodeCanceledLogin)) {
            // Cancelled
            [self sendUnityCallback:"CallFromAuthCallbackError" message: ""];
        } else if (error != nil) {
            [self sendUnityCallback:"CallFromAuthCallbackError" message:error.localizedDescription.UTF8String];
        } else {
            [self sendUnityCallback:"CallFromAuthCallback" message: callbackURL.absoluteString.UTF8String];
        }
    }];

    _authSession.presentationContextProvider = self;
    [_authSession start];
}

- (nonnull ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(nonnull ASWebAuthenticationSession *)session {
    return NSApplication.sharedApplication.windows.firstObject;
}

@end

extern "C" {
void *_CWebViewPlugin_Init(const char *ua);
void _CWebViewPlugin_Destroy(void *instance);
void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback);
void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url, const char *redirectUri);
}

void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback) {
    delegateCallback = callback;
}

void *_CWebViewPlugin_Init(const char *ua)
{
    CWebViewPlugin *webViewPlugin = [[CWebViewPlugin alloc] initWithUa:ua];
    [_instances addObject:webViewPlugin];
    return (__bridge_retained void *)webViewPlugin;
}

void _CWebViewPlugin_Destroy(void *instance)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge_transfer CWebViewPlugin *)instance;
    [_instances removeObject:webViewPlugin];
    [webViewPlugin dispose];
    webViewPlugin = nil;
}

void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url, const char *redirectUri)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin launchAuthURL:url redirectUri: redirectUri];
}
