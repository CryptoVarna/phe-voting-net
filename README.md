<p align="center">
<h1>Privacy Preserving Voting System using Partial Homomorphic Encryption</h1>
<h5>.NET Core Version</h5>
</p>

# Introduction

I will introduce a voting system utilizing Partial Homomorphic Encryption to preserve full voting privacy.
The main goal was to create a voting system where the identity and the vote of the voters remain hidden from anyone in the network. The suggested system relies on partial homomorhpic encryption in order to collect and process all the votes preserving their privacy. I used Paillier cryptosystem but similar results could be achieved with ElGamal for example. Even though the proposed system targets voting it can be used for other purposes such quizes or surveys.
The system is designed for both server and client side thus making it suitable for decentralized systems such as blockchain based.

## Installation

```bash
dotnet add package CryptoVarna.PheVotingNet
```

1. [General Flow](#general-flow)
1. [Vote Encoding](#vote-encoding)
1. [Paillier Cryptosystem](#paillier-cryptosystem)
1. [The Final Solution](#the-final-solution)

# General Flow

<p align="center">
  <img src="assets/pheVoting-gf.svg" alt="Diagram1">
</p>

The process here for each role is:

### Organizer

The party that is organizing the ballot. It has the responsibility to generate a secure public/private key pair and to share the public part to all participants and aggregators.
The organizer is the only one that can see the results. That's why it is important to separate vote collection from it.
When all the votes are aggregated by the aggregator they will be submitted in an encrypted form to the Organizer where they will be decrypted using its private key.
Then the Organizer can finally announce the results.

### Voter

Voters use software to prepare and cast their votes. The software reasds the rules for the ballot - candidates, public key etc. Then the voter chooses a candidate and encodes and encrypts the choice using the Organizer's public key. Then the vote is cast to the Aggregator.

### Aggregator

Aggregator (one ore more) are collecting all votes from the voters. First they need to validate the vote using the zero-knowledge proof provided by the voter. In this way they can see if the vote (selected candidate) is one of the valid candidates without being able to see which one exactly. If correct the vote is aggregated with all other votes and stored. When all votes are done the result is sent to the Organizer. It is important to mention that the Aggregators and the Organizer must be independent from each other otherwise the Aggregators can read the user vote.

In the next chapters we will see how to implement that process.

# Vote Encoding

Let's have 3 candidates - Alice, Bob and Carlos. We assign a number to each candidade, starting from 0 - Alice, 1 - Bob and 2 Carlos.
We'd like to encode the votes into a single integer in order to be able to perform arithmetic operations over it and to encrypt it with the Paillier cryptosystem.

<p align="center">
    <img src="assets/pheVoting-e1.svg" alt="Diagram2">
</p>

We reserve 8 bits for each candidate and since we have 3 candidates we get 3 times by 8 bits in total. So each 8 bit space in our integer (of total 24 bits) will contain the votes cast for each corresponding candidate. The first voter cast a vote for Alice. So we count 1 in the Alice's bit space.

<p align="center">
    <img src="assets/pheVoting-e2.svg" alt="Diagram3">
</p>

The second one votes for Carlos so we add 1 in the Carlos's space. This of course can be achieved using 3 8 bit unsigned integers but to optimize it we can use a single one and just add

```math
2^{numberOfBitsPerVote \times candidateId} = 2^{8 \times 2} = 2^{16} = 65536
```

or

```math
 1 << (numberOfBitsPerVote \times candidateId)
```

<p align="center">
    <img src="assets/pheVoting-e3.svg" alt="Diagram4">
</p>

The third one votes for Alice again. So at the end we have 2 votes for Alice, no votes for Bob and a single vote for Carlos.

<p align="center">
    <img src="assets/pheVoting-e4.svg" alt="Diagram4">
</p>

At this point we have the sum of all votes for each candidate encoded into this 24 bit integer. Its value is $2 + (1 << 16) = 65538$.

<p align="center">
    <img src="assets/pheVoting-e5.svg" alt="Diagram4">
</p>

## Usage

```csharp
using CryptoVarna.PheVotingNet;
```

### Encode a Single Choice Vote

```csharp
// 0 - Alice, 1 - Bob, 2 - Carlos
var numChoices = 3;
var choice = 1; // Bob
var bitsPerChoice = 8;
var encoded = VoteEncoder.Encode(choice, numChoices, bitsPerChoice);
// encoded = 256
var decoded = VoteEncoder.Decode(encoded, numChoices, bitsPerChoice);
// deceded = [0, 1, 0]
```

### Encode a Multiple Choice Vote

```csharp
// 0 - Alice, 1 - Bob, 2 - Carlos
var numChoices = 3;
var choices = new int[] { 0, 1 }; // Alice and Bob
var bitsPerChoice = 8;
var encoded = VoteEncoder.Encode(choices, numChoices, bitsPerChoice);
// encoded = 257
var decoded = VoteEncoder.Decode(encoded, numChoices, bitsPerChoice);
// deceded = [1, 1, 0]
```

# Paillier Cryptosystem

Partial homomorphic encryption is a type of encryption scheme that supports computation on encrypted data without fully revealing the plaintext. It enables specific mathematical operations, such as addition or multiplication, to be performed on encrypted values while maintaining confidentiality. While not as versatile as fully homomorphic encryption, which supports arbitrary computations, partial homomorphic encryption offers a balance between privacy and computation capabilities, making it useful in scenarios where limited calculations on encrypted data are required.
The algorithm used here is the Paillier cryptosystem named after its creator - Pascal Paillier.

**DISCLAIMER: The following is a naïve Paillier cryptosystem implementation and is unaudited. It should not be used for security-critical applications.**

## Usage

```csharp
using CryptoVarna.PheVotingNet;
```

### Generating Key Pair

```csharp
using (var p = new Paillier())
{
    var (pub, priv) = p.GenerateKeyPair(256);
    // perform some encryption
}
```

For real secure scenarios use key sizes of 3072 bits, 4096 bits or more. See [here](https://www.keylength.com/en/4/) for more info.

### Encryption

```csharp
var plainMessage = BigInteger.Parse("12345");
var encryptedMessage = p.Encrypt(plainMessage, pub);
```

### Decryption

```csharp
var decryptedMessage = p.Decrypt(encryptedMessage, pub, priv);
// decryptedMessage = 12345
```

### Addition of Encrypted Numbers

```csharp
var a = BigInteger.Parse("1");
var b = BigInteger.Parse("2");
var aEncrypted = p.Encrypt(a, pub);
var bEncrypted = p.Encrypt(b, pub);
var sumEncrypted = p.AddEncrypted(aEncrypted, bEncrypted, pub);
var sum = p.Decrypt(sumEncrypted, pub, priv);
// sum = 3
```

### Addition of Encrypted and Scalar

```csharp
var a = BigInteger.Parse("1");
var b = BigInteger.Parse("2");
var aEncrypted = p.Encrypt(a, pub);
var sumEncrypted = p.AddEncrypted(aEncrypted, b, pub);
var sum = p.Decrypt(sumEncrypted, pub, priv);
// sum = 3
```

### Multiplication of Encrypted and Scalar

```csharp
var a = BigInteger.Parse("2");
var b = BigInteger.Parse("5");
var aEncrypted = p.Encrypt(a, pub);
var prodEncrypted = p.MulScalar(aEncrypted, b, pub);
var prod = p.Decrypt(prodEncrypted, pub, priv);
// prod = 10
```

### Signing and Verifying

```csharp
var m = BigInteger.Parse("12345");
var sig = p.CreateSignature(m, pub, priv);
// Send m and sig to the corresponding party
var valid = p.VerifySignature(m, sig, pub);
// valid should be true
```

### Encrypting with Zero-Knowledge Proof

```csharp
var m = BigInteger.Parse("12345");
var valid = new List<BigInteger> { 1, 12345 };
var invalid = new List<BigInteger> { 1, 2 };
var (c, commitment) = p.EncryptWithZkp(m, valid, pub);
p.VerifyZkp(c, valid, commitment, pub); // true
p.VerifyZkp(c, invalid, commitment, pub); // false
```

# The Final Solution

```csharp
using CryptoVarna.PheVotingNet;

var numChoices = 3;
var bitsPerChoice = 8;
var numVotes = 99;

using (var p = new Paillier())
{
    // *****************
    // *** Organizer ***
    // *****************
    var (pub, priv) = p.GenerateKeyPair(160);

    var validVotes = VoteEncoder.GetSingleChoicePermutations(numChoices, bitsPerChoice);
    var realVotes = new int[validVotes.Count];

    var encryptedSum = p.Encrypt(BigInteger.Zero, pub);

    for (var i = 0; i < numVotes; i++)
    {
        // *************
        // *** Voter ***
        // *************
        // Choose one of the candidates
        var vote = i % numChoices;
        // Encode vote
        var encodedVote = VoteEncoder.Encode(vote, numChoices, bitsPerChoice);
        // Encrypt vote
        var (encryptedVote, commitment) = p.EncryptWithZkp(encodedVote, validVotes, pub);

        // ******************
        // *** Aggregator ***
        // ******************
        // Check vote validity
        var valid = p.VerifyZkp(encryptedVote, validVotes, commitment, pub);
        if (!valid) throw new Error("Invalid vote");
        // Aggregate votes
        encryptedSum = p.AddEncrypted(encryptedSum, encryptedVote, pub);
    }

    // *****************
    // *** Organizer ***
    // *****************
    BigInteger decryptedSum = p.Decrypt(encryptedSum, pub, priv);
    var decodedSum = VoteEncoder.Decode(decryptedSum, numChoices, bitsPerChoice);
    // decodedSum = [33, 33, 33]
}
```

# Further Developments

Even though the proposed system can be used there are some additions that can be made to improve its security.

### Hiding voter's identity from the Aggregator

If the voter communicates directly with the Aggregator using software connected to internet then at least the IP address of the voter can be seen by the aggregator thus lowering the privacy of the voter. This can be secured in different ways one of which is using an Onion Router such as [Tor](<https://en.wikipedia.org/wiki/Tor_(network)>) or similar network.

### Decentralization

If we introduce the blockchain techologies into the voting system we can achieve much higher level of privacy and independancy. For example all ballots can be announced on chain along with the public key of the organizer. Everyone can see this information and no connection to the organizer will be required.
Aggregators could be anyone following the protocol. At the end all aggregators can deposit partial results on chain and since they are encrypted no one excepts the organizer can see them.

# References

- [Paillier Cryptosystem - Wiki](https://en.wikipedia.org/wiki/Paillier_cryptosystem)
- [Paillier Cryptosystem - Original Paper](https://www.cs.tau.ac.il/~fiat/crypt07/papers/Pai99pai.pdf)
- [Zero-Knowledge Proof in Paillier Cryptosystem](https://paillier.daylightingsociety.org/Paillier_Zero_Knowledge_Proof.pdf)
