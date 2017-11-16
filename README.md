# BlockSaver SmartSavingContract

## Introduction

## Development

## Testing

Neo-python commands for contract invoking:

//create
`testInvoke 20dd61645c511e8e1d5c12a722a65db7420ed798 "637265617465536176696e6773" [“6c6f6f70”,1510488386]`

//all savings
`testInvoke 20dd61645c511e8e1d5c12a722a65db7420ed798 "676574416c6c536176696e6773" []`

//savings details
`testInvoke 20dd61645c511e8e1d5c12a722a65db7420ed798 "676574536176696e677342794e616d65" [“6c6f6f70”]`

//transfer
`testInvoke 20dd61645c511e8e1d5c12a722a65db7420ed798 "7472616e73666572" 74657374c 1510488386 --attach-neo=1 --attach-gas=1`


### Private Network
```json
{
  "ProtocolConfiguration": {
    "Magic": 56753,
    "AddressVersion": 23,
    "StandbyValidators": [
      "02b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc2",
      "02103a7f7dd016558597f7960d27c516a4394fd968b9e65155eb4b013e4040406e",
      "03d90c07df63e690ce77912e10ab51acc944b66860237b608c4f8f8309e71ee699",
      "02a7bc55fe8684e0119768d104ba30795bdcc86619e864add26156723ed185cd62"

    ],
    "SeedList": [
      "188.226.138.245:20333",
      "188.226.138.245:20334",
      "188.226.138.245:20335",
      "188.226.138.245:20336"
    ],
    "SystemFee": {
      "EnrollmentTransaction": 1000,
      "IssueTransaction": 500,
      "PublishTransaction": 500,
      "RegisterTransaction": 10000
    }

  }
}
```
Versions:

| Version | Contract Hash |
|---------|---------------|
|     v0.0.1   |    20dd61645c511e8e1d5c12a722a65db7420ed798           |
|     v0.0.2   |    3d428cf6137bb0e09b82125a27dfc61a29723394           |
