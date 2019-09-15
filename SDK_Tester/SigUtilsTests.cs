using Gigya.Socialize.SDK;
using NUnit.Framework;

namespace SDK_Tester
{
    [TestFixture]
    public class SigUtilsTests
    {
        [Test]
        public void AuthorizationHeaderTests()
        {
            const string pem = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA4fsa42M7J6J/ci2EXhtauY5oLGwmPeaPR0xjs6ufdnOnpQGY
9dFbd07NV59+FztbKTdkUZyhuAcT99oINYLoxaWXmdk5K/+pqh/ZuEy9CEq861TY
3uUJ3XN8gBWKEmU6AQQsCDZDIsE/o2VJxtvjhZxgjMshRoDB0DXjuVlRF5Qi5Om8
0sXRJiXqmdcsQ3QY8e7ivG+t/sNA3K6+fvRifty2v6x7n5IpfieT5pVhvQDa4JsS
9+w9sM2sRroP2s6FxMA1QRyvc9i1LV1YA/CNKtCZ7DjIqlupJWF7LIrtZStvV+Xg
WtoQ63b+EIJ5Tu5gk4XiUf3jBjL2wHMx5kC2rQIDAQABAoIBAAcpAjSzmT5pktyz
UrlWMTDBTOjp/jF9aV23SjxwMLYJSQXhK3irVDRGBYjR5ThZlN15o4NFw8a6EK+n
7vCVHa04rBTbCNKtdrrM6Fbe2il6WQrZsrS6J3f5Ey776N/gjbgNBlNs/90R8T7L
BrIXKeMfgADynAUyHLaR27JIOCwXj7lw6FqLxR3gcq8Eb8orTiKFXM9NHtYLp+Ac
/j0rekgtI+5CJY3htJ7A6dSttE+lCt6G2uGwr58Yu/3mMQ+9Vu5n1uBfrczzX1k2
ThYPFk9nIQH65TG7DHaB6c/FwJwC3QiAcJtnp+opvH9I+eJ7Oi5+iaM0W8+0KSPm
9IHR/YkCgYEA6JPf0aCs0X+dVhzDKces3kKsdY8L2Q5iv/7XPJGP3sXxTJqNvcLd
ygyVUfyRIp+RZ6PpkAGYU2yaIKHjT7NWJ3nK5ZaZ+01Q5nevi04x5JBGyMK04kmt
YEwmhIpF433gplk4p86LgP06v0qj3aKe45tSut34fTPRIfduhn5e4MUCgYEA+L0o
mykQsBbGJu4fOKqNWbGkTFivEMJC+CuuEVQZhDWul6uvs13Oan3CSHsUiwgL277X
z5FYzba3rzHCOXxQVDcPD12eoK6u52U0gYdorTOZzCojonXEcDQycsu6iW2x9UYx
RgpKex5EFs8pvaxkwHqXmqXyS7xKqiBP9S/lDMkCgYAJlAV0saRMYHAPWtHix5lj
8eT+VmzLfJ8ufwVINkpxhz9fw0GxHfRaXNhNbxRfE6k+Vm7JAnfOf7t9Oo2M+7rB
l292sxQWWGHLjARLvWWqnxJ7NCGU7CnavGgdr0AflVCKKUR/DK+MGWGw/RbwisD2
aLAoh/my1k53kqQXn96ybQKBgQC9FD17xPmUgZtbGIPPNYaBehHknz1kxebWc428
SmujHpN7Y90JwfMY7EP1iOoSzakF/8pZVKlmptB2cqKrxB3kBn6CNa5Rgrgd2cbR
97bQgnsUwauY4WDT0jnPHaLMuQAf7J2kGkqH0Hf9xrh6IEPuNMJtolvOynEPZcSi
IyhAUQKBgBfxPIAYhAQYKIc3zqg0Q6HnStLgw3eywc1PEWI5vlDZ1ZK4SQT9gtVY
TetAnIqbiQwSEauF0oLqq74MDm0fz3B4HqRF+LTYocKWYd2MtcY+QhdvMuWRIbJa
nA0BonbD1x0I0uyzC1KJMWy8mJn2X588RteIWvIWzFnFQJcWZJEP
-----END RSA PRIVATE KEY-----";

            
            Assert.DoesNotThrow(() => SigUtils.CalcAuthorizationBearer("someUserKey", pem));
        }
        
    }
}