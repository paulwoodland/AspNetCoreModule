// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace AspNetCoreModule.Test.WebSocketClient
{
    public class Frames
    {
        public static byte[] CLOSE_FRAME = new byte[] { 0x88, 0x85, 0xBD, 0x60, 0x97, 0x72, 0xBE, 0x88, 0xA5, 0x40, 0x8F };
        public static byte[] PING = new byte[] { 0x89, 0x88, 0,0,0,0, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB };
        public static byte[] PONG = new byte[] { 0x8A, 0x08, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB, 0xBB };
        public static byte[] HELLO = new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f };
        public static byte[] FRAME_4096 = new byte[] { 0x81, 0xFE, 0x10, 0x00, 0x88, 0x48, 0x9B, 0xE7, 0xDA, 0x0D, 0xD4, 0xB7, 0xCA, 0x0E, 0xD2, 0xA1, 0xD2, 0x0C, 0xD7, 0xB0, 0xCB, 0x1A, 0xC9, 0xB4, 0xC0, 0x1B, 0xD8, 0xA8, 0xC5, 0x05, 0xDC, 0xBF, 0xC7, 0x19, 0xC2, 0xAD, 0xD0, 0x1B, 0xC9, 0xA8, 0xDF, 0x0E, 0xDF, 0xB4, 0xD8, 0x12, 0xDF, 0xBD, 0xC6, 0x04, 0xCD, 0xB5, 0xD9, 0x05, 0xDA, 0xBD, 0xC5, 0x1B, 0xD9, 0xBF, 0xCD, 0x1F, 0xDF, 0xA2, 0xD0, 0x1A, 0xDA, 0xAC, 0xC0, 0x12, 0xD2, 0xA0, 0xC4, 0x10, 0xDA, 0xB0, 0xC7, 0x11, 0xD8, 0xAE, 0xD8, 0x0B, 0xD9, 0xA5, 0xDA, 0x18, 0xD3, 0xA4, 0xD8, 0x12, 0xCC, 0xA1, 0xDA, 0x1D, 0xD0, 0xA6, 0xCB, 0x11, 0xCE, 0xBE, 0xD2, 0x1B, 0xC9, 0xA9, 0xCB, 0x07, 0xDD, 0xB3, 0xC5, 0x0D, 0xD7, 0xA9, 0xD8, 0x1C, 0xD2, 0xA5, 0xDD, 0x0B, 0xD5, 0xAE, 0xC3, 0x11, 0xCD, 0xAE, 0xCB, 0x0C, 0xCB, 0xB5, 0xC5, 0x12, 0xCE, 0xB7, 0xCD, 0x0D, 0xD2, 0xB0, 0xC3, 0x06, 0xC2, 0xA3, 0xC3, 0x06, 0xCB, 0xAB, 0xC6, 0x02, 0xCB, 0xBE, 0xC4, 0x01, 0xDD, 0xBD, 0xC4, 0x05, 0xD5, 0xA5, 0xDF, 0x02, 0xD7, 0xBD, 0xD1, 0x07, 0xDC, 0xAA, 0xC3, 0x1E, 0xD1, 0xAB, 0xC3, 0x04, 0xCA, 0xAF, 0xCD, 0x02, 0xC2, 0xB0, 0xC1, 0x03, 0xCE, 0xB4, 0xC7, 0x1A, 0xDD, 0xA3, 0xDD, 0x1D, 0xDE, 0xB5, 0xD9, 0x00, 0xDA, 0xA5, 0xCA, 0x12, 0xDE, 0xB1, 0xC0, 0x0C, 0xDA, 0xB4, 0xC9, 0x0D, 0xD5, 0xA6, 0xDB, 0x10, 0xCD, 0xA5, 0xC7, 0x1A, 0xC8, 0xAA, 0xD8, 0x1C, 0xD0, 0xAF, 0xC1, 0x0B, 0xC8, 0xB7, 0xDA, 0x1A, 0xCE, 0xA4, 0xC4, 0x18, 0xDC, 0xA1, 0xCD, 0x0B, 0xCB, 0xA1, 0xC2, 0x0B, 0xC8, 0xAC, 0xCC, 0x0E, 0xD7, 0xB0, 0xD2, 0x0B, 0xDF, 0xBD, 0xD8, 0x07, 0xD6, 0xAF, 0xC7, 0x10, 0xD5, 0xA2, 0xC4, 0x04, 0xD9, 0xAE, 0xC2, 0x03, 0xD4, 0xA3, 0xDA, 0x19, 0xCC, 0xAA, 0xCB, 0x06, 0xD8, 0xA9, 0xCA, 0x09, 0xDF, 0xA3, 0xDA, 0x1E, 0xCA, 0xA8, 0xC7, 0x1E, 0xD5, 0xBF, 0xCC, 0x11, 0xCA, 0xAF, 0xC7, 0x03, 0xCE, 0xBE, 0xCA, 0x02, 0xDA, 0xB4, 0xD9, 0x01, 0xDD, 0xAE, 0xCE, 0x19, 0xCC, 0xA5, 0xC4, 0x12, 0xDC, 0xA9, 0xDC, 0x03, 0xD5, 0xB7, 0xDE, 0x05, 0xCE, 0xA9, 0xD0, 0x1A, 0xDA, 0xB4, 0xD2, 0x18, 0xC3, 0xB5, 0xDC, 0x0D, 0xD6, 0xB4, 0xC9, 0x04, 0xD6, 0xAE, 0xD8, 0x00, 0xD2, 0xBF, 0xD1, 0x05, 0xD7, 0xA2, 0xDA, 0x0B, 0xD4, 0xA2, 0xDF, 0x0D, 0xD8, 0xA8, 0xD8, 0x05, 0xCD, 0xBE, 0xC1, 0x05, 0xD2, 0xB6, 0xDC, 0x0E, 0xD2, 0xA0, 0xC5, 0x07, 0xD5, 0xAE, 0xD0, 0x0E, 0xDA, 0xA9, 0xCD, 0x1F, 0xD4, 0xAB, 0xCF, 0x1F, 0xD9, 0xBF, 0xC9, 0x1B, 0xCE, 0xB7, 0xCA, 0x10, 0xD8, 0xA9, 0xD8, 0x07, 0xDA, 0xA3, 0xD2, 0x1A, 0xDE, 0xB3, 0xCC, 0x0D, 0xC9, 0xA3, 0xD8, 0x0F, 0xDC, 0xB5, 0xCC, 0x18, 0xD0, 0xB3, 0xD1, 0x03, 0xC8, 0xAA, 0xC4, 0x04, 0xCB, 0xA6, 0xC3, 0x1C, 0xDD, 0xB7, 0xC4, 0x09, 0xC8, 0xAD, 0xCD, 0x10, 0xD4, 0xAA, 0xDA, 0x1E, 0xD3, 0xA5, 0xCE, 0x10, 0xD3, 0xB2, 0xC5, 0x0B, 0xD7, 0xAA, 0xC6, 0x02, 0xCB, 0xA1, 0xDE, 0x07, 0xC9, 0xA9, 0xCA, 0x0D, 0xD2, 0xAC, 0xD2, 0x0A, 0xC9, 0xA8, 0xC7, 0x10, 0xD5, 0xA0, 0xCA, 0x10, 0xD9, 0xA4, 0xCB, 0x1A, 0xD3, 0xA8, 0xCC, 0x1E, 0xD4, 0xAF, 0xC1, 0x1C, 0xD9, 0xA5, 0xC4, 0x05, 0xD5, 0xB6, 0xCE, 0x0A, 0xD0, 0xA9, 0xC5, 0x1F, 0xD9, 0xA3, 0xCE, 0x1C, 0xDC, 0xA8, 0xD8, 0x0D, 0xD7, 0xB7, 0xC1, 0x05, 0xD8, 0xA2, 0xC0, 0x0C, 0xD1, 0xA0, 0xD8, 0x09, 0xD8, 0xA0, 0xC9, 0x19, 0xDF, 0xA5, 0xC2, 0x1F, 0xD9, 0xBD, 0xC6, 0x06, 0xCB, 0xA1, 0xD8, 0x0C, 0xD2, 0xAC, 0xC7, 0x12, 0xC9, 0xA3, 0xC0, 0x04, 0xCF, 0xBE, 0xC2, 0x02, 0xD1, 0xA5, 0xDA, 0x0D, 0xC2, 0xAB, 0xDD, 0x1E, 0xDF, 0xB7, 0xD8, 0x0E, 0xDE, 0xB3, 0xCC, 0x04, 0xD8, 0xB2, 0xDF, 0x10, 0xD4, 0xA3, 0xDE, 0x11, 0xC9, 0xB7, 0xC2, 0x11, 0xC2, 0xA8, 0xDF, 0x0D, 0xC9, 0xBD, 0xC6, 0x12, 0xD2, 0xAD, 0xD8, 0x0C, 0xD7, 0xB2, 0xC9, 0x1B, 0xCE, 0xAD, 0xDE, 0x11, 0xDA, 0xB0, 0xC1, 0x11, 0xD9, 0xAA, 0xDE, 0x0E, 0xDC, 0xB3, 0xC5, 0x01, 0xD8, 0xB1, 0xD0, 0x07, 0xCF, 0xAB, 0xC6, 0x0E, 0xDD, 0xA3, 0xCB, 0x1C, 0xDE, 0xB3, 0xC4, 0x1D, 0xDF, 0xA4, 0xCC, 0x00, 0xCE, 0xAC, 0xD1, 0x0A, 0xDD, 0xBF, 0xCB, 0x0E, 0xDE, 0xAF, 0xDB, 0x18, 0xCC, 0xAF, 0xCA, 0x18, 0xCC, 0xAA, 0xD2, 0x02, 0xCC, 0xB6, 0xDB, 0x1F, 0xCF, 0xB7, 0xDD, 0x01, 0xDA, 0xA6, 0xCB, 0x0D, 0xCB, 0xA2, 0xC6, 0x1B, 0xC3, 0xB1, 0xC1, 0x1E, 0xD1, 0xAF, 0xC9, 0x10, 0xD7, 0xA9, 0xD0, 0x10, 0xC8, 0xB0, 0xD0, 0x19, 0xD8, 0xB2, 0xC4, 0x0D, 0xC9, 0xA5, 0xC0, 0x19, 0xDF, 0xB3, 0xCE, 0x0C, 0xDD, 0xA9, 0xD2, 0x1B, 0xCF, 0xAE, 0xDB, 0x0A, 0xDF, 0xA3, 0xD2, 0x07, 0xCB, 0xB1, 0xC3, 0x0F, 0xC8, 0xB0, 0xD9, 0x0D, 0xDF, 0xAB, 0xCA, 0x1E, 0xC8, 0xAD, 0xC9, 0x1E, 0xD5, 0xB5, 0xDB, 0x18, 0xD8, 0xBF, 0xDB, 0x10, 0xD4, 0xA2, 0xCC, 0x02, 0xDF, 0xB1, 0xC6, 0x11, 0xCE, 0xBF, 0xC7, 0x1F, 0xCA, 0xA5, 0xD1, 0x0F, 0xD8, 0xA6, 0xD2, 0x1C, 0xD6, 0xA2, 0xD2, 0x03, 0xD3, 0xBF, 0xC6, 0x04, 0xD7, 0xAD, 0xC4, 0x1B, 0xD8, 0xA9, 0xDA, 0x06, 0xC8, 0xAF, 0xC9, 0x00, 0xC3, 0xA3, 0xC9, 0x1E, 0xCE, 0xA2, 0xCD, 0x05, 0xCF, 0xAB, 0xC6, 0x0A, 0xC3, 0xBE, 0xC4, 0x02, 0xDD, 0xB3, 0xCA, 0x0F, 0xD2, 0xA4, 0xC6, 0x03, 0xD7, 0xB0, 0xDC, 0x0C, 0xD1, 0xAB, 0xC7, 0x1E, 0xDA, 0xB4, 0xDE, 0x01, 0xDE, 0xA2, 0xD9, 0x0C, 0xCF, 0xA5, 0xDB, 0x09, 0xCC, 0xAD, 0xDE, 0x0C, 0xD9, 0xAF, 0xC1, 0x12, 0xDD, 0xB2, 0xD2, 0x1E, 0xCA, 0xB7, 0xC2, 0x10, 0xD0, 0xA6, 0xCB, 0x00, 0xCC, 0xB5, 0xCA, 0x0D, 0xDF, 0xA4, 0xCE, 0x09, 0xDF, 0xBE, 0xC5, 0x00, 0xD1, 0xAA, 0xC7, 0x0B, 0xD9, 0xB6, 0xCB, 0x0A, 0xDF, 0xA8, 0xD8, 0x0F, 0xCF, 0xBD, 0xDB, 0x07, 0xCE, 0xB4, 0xDB, 0x1B, 0xC2, 0xAC, 0xCC, 0x0D, 0xD3, 0xB6, 0xC9, 0x11, 0xD8, 0xAF, 0xDE, 0x00, 0xD3, 0xB5, 0xC5, 0x0C, 0xDA, 0xAF, 0xDF, 0x1D, 0xC2, 0xA6, 0xCD, 0x01, 0xD8, 0xB1, 0xC4, 0x0C, 0xD1, 0xB5, 0xCF, 0x04, 0xDD, 0xB2, 0xC2, 0x11, 0xD1, 0xAD, 0xDD, 0x04, 0xCB, 0xA3, 0xD2, 0x1E, 0xCF, 0xAE, 0xD1, 0x0A, 0xD5, 0xB7, 0xC6, 0x06, 0xCD, 0xBF, 0xDD, 0x10, 0xDC, 0xB1, 0xCB, 0x05, 0xDE, 0xBF, 0xD8, 0x03, 0xD9, 0xAC, 0xCA, 0x06, 0xD2, 0xA9, 0xDD, 0x19, 0xD6, 0xAB, 0xCD, 0x1E, 0xD9, 0xAD, 0xC7, 0x1C, 0xCC, 0xAD, 0xD9, 0x1D, 0xDF, 0xB4, 0xD8, 0x00, 0xC1, 0xAB, 0xDB, 0x06, 0xD3, 0xAF, 0xCF, 0x1B, 0xD4, 0xA8, 0xDD, 0x01, 0xD3, 0xAB, 0xDC, 0x12, 0xCE, 0xB0, 0xC9, 0x03, 0xCF, 0xBD, 0xDF, 0x10, 0xDC, 0xAE, 0xD9, 0x1E, 0xDC, 0xB2, 0xC0, 0x01, 0xCD, 0xB3, 0xC6, 0x10, 0xCD, 0xA0, 0xC2, 0x0E, 0xDE, 0xAA, 0xC0, 0x05, 0xD4, 0xA0, 0xC5, 0x03, 0xCA, 0xB6, 0xD2, 0x0F, 0xC8, 0xA2, 0xC7, 0x09, 0xCB, 0xB1, 0xC0, 0x11, 0xC9, 0xAB, 0xC4, 0x1C, 0xD3, 0xAA, 0xC5, 0x06, 0xC2, 0xB1, 0xCD, 0x07, 0xD5, 0xB1, 0xCE, 0x0F, 0xC8, 0xAC, 0xC1, 0x12, 0xCC, 0xA1, 0xCD, 0x19, 0xCD, 0xA5, 0xD9, 0x19, 0xDE, 0xAA, 0xC0, 0x09, 0xC1, 0xAB, 0xC6, 0x1C, 0xDA, 0xA9, 0xCE, 0x0B, 0xCF, 0xBE, 0xC5, 0x1D, 0xD6, 0xAB, 0xDC, 0x1F, 0xC1, 0xAF, 0xC1, 0x0E, 0xD6, 0xAF, 0xCA, 0x04, 0xDC, 0xB1, 0xD1, 0x0F, 0xCD, 0xB0, 0xC1, 0x05, 0xD4, 0xA3, 0xC7, 0x0A, 0xD3, 0xAB, 0xCF, 0x0D, 0xDD, 0xA0, 0xCE, 0x10, 0xCF, 0xAD, 0xCC, 0x02, 0xD3, 0xB3, 0xDA, 0x1F, 0xDF, 0xA4, 0xC6, 0x1B, 0xD1, 0xA5, 0xC6, 0x0E, 0xCB, 0xBE, 0xCF, 0x10, 0xCA, 0xBD, 0xCF, 0x01, 0xCC, 0xB5, 0xC7, 0x06, 0xD9, 0xA2, 0xC9, 0x0F, 0xD9, 0xA2, 0xDB, 0x10, 0xC9, 0xA8, 0xD1, 0x0B, 0xDD, 0xAA, 0xC1, 0x04, 0xCA, 0xB0, 0xDB, 0x0F, 0xC2, 0xA5, 0xD8, 0x0F, 0xC1, 0xAE, 0xC0, 0x1C, 0xD8, 0xB1, 0xC6, 0x19, 0xDE, 0xA3, 0xDE, 0x12, 0xD8, 0xA0, 0xD9, 0x0D, 0xD1, 0xB7, 0xC6, 0x09, 0xDA, 0xA2, 0xDB, 0x0C, 0xC9, 0xB1, 0xDA, 0x09, 0xC2, 0xAE, 0xC7, 0x09, 0xD4, 0xB3, 0xC0, 0x1B, 0xC3, 0xBE, 0xDD, 0x1F, 0xD9, 0xAF, 0xD0, 0x0A, 0xC9, 0xAE, 0xC1, 0x02, 0xDD, 0xA9, 0xDF, 0x02, 0xD5, 0xA8, 0xCE, 0x1E, 0xCA, 0xA3, 0xCB, 0x00, 0xDF, 0xAA, 0xDA, 0x1E, 0xD4, 0xB2, 0xC3, 0x01, 0xC1, 0xBE, 0xC0, 0x03, 0xCD, 0xB6, 0xD2, 0x1C, 0xDC, 0xB6, 0xC5, 0x01, 0xD5, 0xAF, 0xDD, 0x04, 0xD6, 0xA1, 0xC5, 0x09, 0xD4, 0xAA, 0xCB, 0x1C, 0xCC, 0xA9, 0xDC, 0x07, 0xCA, 0xA4, 0xC6, 0x1F, 0xC2, 0xBD, 0xC3, 0x00, 0xDC, 0xAB, 0xC7, 0x1F, 0xD4, 0xAA, 0xC7, 0x09, 0xCA, 0xB3, 0xDD, 0x1E, 0xC8, 0xA0, 0xC2, 0x01, 0xD3, 0xAC, 0xDD, 0x06, 0xCD, 0xA9, 0xC7, 0x00, 0xC3, 0xAB, 0xCC, 0x0D, 0xDF, 0xB6, 0xC3, 0x06, 0xC3, 0xA9, 0xCD, 0x09, 0xC9, 0xB7, 0xC4, 0x0B, 0xC3, 0xA5, 0xCB, 0x0B, 0xCF, 0xBE, 0xDF, 0x02, 0xC8, 0xA2, 0xC7, 0x07, 0xDD, 0xBF, 0xC4, 0x1B, 0xC3, 0xA0, 0xD2, 0x0B, 0xD1, 0xAC, 0xD0, 0x09, 0xD1, 0xA0, 0xD0, 0x0D, 0xD9, 0xAD, 0xD9, 0x1A, 0xC2, 0xB5, 0xD8, 0x1D, 0xD7, 0xAB, 0xC6, 0x11, 0xCB, 0xB3, 0xC4, 0x11, 0xD9, 0xB0, 0xC0, 0x12, 0xD8, 0xAA, 0xCC, 0x04, 0xCA, 0xAD, 0xDC, 0x05, 0xDE, 0xA4, 0xDC, 0x05, 0xDA, 0xB5, 0xC0, 0x02, 0xD5, 0xB0, 0xD8, 0x06, 0xD3, 0xB5, 0xCF, 0x04, 0xC8, 0xA5, 0xC5, 0x18, 0xC1, 0xBE, 0xD1, 0x06, 0xC2, 0xBE, 0xCB, 0x18, 0xDD, 0xA1, 0xCA, 0x07, 0xC2, 0xA3, 0xD8, 0x02, 0xC8, 0xA6, 0xD0, 0x11, 0xD6, 0xA4, 0xC4, 0x0D, 0xDC, 0xB2, 0xDA, 0x04, 0xDD, 0xB4, 0xDB, 0x18, 0xCC, 0xA3, 0xC6, 0x0F };
        public static byte[] FRAME_5000 = new byte[] { 0x81, 0xFE, 0x13, 0x88, 0x17, 0x84, 0x25, 0xB2, 0x58, 0xC6, 0x71, 0xE0, 0x4E, 0xD5, 0x77, 0xE0, 0x50, 0xD0, 0x69, 0xFA, 0x43, 0xCF, 0x75, 0xFF, 0x5C, 0xCB, 0x60, 0xE1, 0x52, 0xD0, 0x70, 0xFE, 0x5D, 0xCE, 0x71, 0xF3, 0x44, 0xC7, 0x60, 0xF0, 0x58, 0xC8, 0x6D, 0xE8, 0x5E, 0xDE, 0x60, 0xFF, 0x51, 0xCA, 0x6A, 0xF0, 0x44, 0xDE, 0x62, 0xE6, 0x59, 0xC5, 0x67, 0xEB, 0x4E, 0xC1, 0x77, 0xE4, 0x50, 0xCC, 0x6D, 0xE2, 0x40, 0xD5, 0x7C, 0xF6, 0x58, 0xCF, 0x76, 0xFA, 0x54, 0xD4, 0x60, 0xFE, 0x5D, 0xD6, 0x68, 0xE0, 0x52, 0xD0, 0x71, 0xF9, 0x54, 0xDE, 0x6B, 0xE0, 0x55, 0xC3, 0x66, 0xF8, 0x42, 0xC8, 0x71, 0xF3, 0x45, 0xD4, 0x75, 0xFD, 0x58, 0xC8, 0x68, 0xFA, 0x50, 0xDE, 0x77, 0xEA, 0x40, 0xD4, 0x6A, 0xF5, 0x44, 0xC5, 0x74, 0xFC, 0x58, 0xDC, 0x68, 0xEA, 0x53, 0xC3, 0x67, 0xFB, 0x5F, 0xCE, 0x6B, 0xE3, 0x40, 0xC0, 0x71, 0xE6, 0x55, 0xDC, 0x66, 0xE6, 0x50, 0xC8, 0x61, 0xF1, 0x5E, 0xD4, 0x73, 0xFE, 0x45, 0xD2, 0x77, 0xE6, 0x41, 0xC2, 0x68, 0xE7, 0x54, 0xD7, 0x69, 0xFA, 0x5D, 0xC1, 0x7F, 0xEA, 0x5A, 0xC5, 0x67, 0xE6, 0x40, 0xD2, 0x63, 0xE7, 0x4F, 0xDC, 0x62, 0xF6, 0x43, 0xCE, 0x6A, 0xFC, 0x5B, 0xD4, 0x74, 0xFF, 0x45, 0xD0, 0x73, 0xE3, 0x46, 0xDD, 0x74, 0xFB, 0x5A, 0xD2, 0x6F, 0xF1, 0x5B, 0xC3, 0x74, 0xFB, 0x58, 0xC7, 0x75, 0xE5, 0x46, 0xDC, 0x72, 0xEA, 0x4E, 0xCE, 0x67, 0xE6, 0x52, 0xDC, 0x72, 0xE7, 0x59, 0xCA, 0x63, 0xE1, 0x52, 0xCF, 0x66, 0xEA, 0x52, 0xD3, 0x6D, 0xF1, 0x58, 0xC0, 0x77, 0xFC, 0x43, 0xC3, 0x7F, 0xF8, 0x56, 0xD0, 0x73, 0xE7, 0x4F, 0xDD, 0x76, 0xFA, 0x4F, 0xDC, 0x60, 0xFD, 0x4D, 0xCB, 0x6A, 0xEA, 0x56, 0xDC, 0x61, 0xF7, 0x4D, 0xD6, 0x76, 0xE6, 0x47, 0xD0, 0x6C, 0xE7, 0x45, 0xCB, 0x7F, 0xEB, 0x4E, 0xC9, 0x71, 0xE7, 0x44, 0xCF, 0x73, 0xF5, 0x44, 0xD1, 0x64, 0xF5, 0x44, 0xD7, 0x61, 0xE8, 0x58, 0xD1, 0x68, 0xE4, 0x54, 0xD1, 0x6F, 0xFB, 0x56, 0xC7, 0x63, 0xF6, 0x47, 0xDC, 0x74, 0xF8, 0x4F, 0xC2, 0x74, 0xFF, 0x41, 0xD1, 0x63, 0xE3, 0x54, 0xD3, 0x68, 0xF4, 0x46, 0xC9, 0x64, 0xE5, 0x46, 0xCE, 0x63, 0xEA, 0x55, 0xC1, 0x73, 0xF6, 0x54, 0xC8, 0x71, 0xE3, 0x52, 0xD7, 0x77, 0xE7, 0x52, 0xD6, 0x6C, 0xFF, 0x54, 0xD5, 0x60, 0xE7, 0x58, 0xD3, 0x76, 0xF5, 0x5E, 0xC1, 0x77, 0xFD, 0x55, 0xCE, 0x6B, 0xF4, 0x45, 0xD0, 0x6D, 0xE6, 0x5C, 0xC9, 0x6F, 0xF8, 0x55, 0xD4, 0x69, 0xF9, 0x52, 0xD6, 0x67, 0xE8, 0x53, 0xCB, 0x71, 0xE8, 0x52, 0xC8, 0x6C, 0xF4, 0x5B, 0xCB, 0x73, 0xEB, 0x43, 0xC1, 0x75, 0xE4, 0x51, 0xC8, 0x61, 0xF9, 0x5C, 0xD4, 0x66, 0xE2, 0x5F, 0xD1, 0x76, 0xE8, 0x5C, 0xCD, 0x67, 0xE0, 0x53, 0xD7, 0x6E, 0xFF, 0x47, 0xCA, 0x64, 0xF4, 0x5C, 0xC6, 0x6D, 0xE4, 0x45, 0xC8, 0x74, 0xE5, 0x56, 0xD4, 0x63, 0xE6, 0x59, 0xD5, 0x6A, 0xFD, 0x5B, 0xC0, 0x76, 0xF9, 0x44, 0xCD, 0x70, 0xF6, 0x59, 0xC1, 0x73, 0xF3, 0x43, 0xC7, 0x62, 0xE0, 0x5B, 0xDD, 0x7F, 0xFB, 0x5F, 0xCC, 0x7C, 0xE5, 0x52, 0xD2, 0x7F, 0xE4, 0x53, 0xCD, 0x61, 0xFF, 0x53, 0xD3, 0x67, 0xFE, 0x41, 0xD5, 0x68, 0xF1, 0x5F, 0xC1, 0x6D, 0xFF, 0x47, 0xD4, 0x61, 0xEA, 0x5D, 0xCA, 0x6D, 0xE2, 0x46, 0xC3, 0x6D, 0xF7, 0x51, 0xD2, 0x62, 0xEA, 0x5D, 0xDE, 0x7F, 0xF7, 0x55, 0xCD, 0x72, 0xEA, 0x56, 0xD1, 0x72, 0xE7, 0x5B, 0xDC, 0x67, 0xF6, 0x4D, 0xC8, 0x62, 0xFD, 0x44, 0xC6, 0x69, 0xE2, 0x56, 0xCA, 0x72, 0xEA, 0x47, 0xDC, 0x62, 0xE8, 0x5D, 0xD4, 0x71, 0xFA, 0x52, 0xC0, 0x69, 0xFA, 0x43, 0xC2, 0x7D, 0xE2, 0x45, 0xCA, 0x60, 0xE1, 0x51, 0xC0, 0x60, 0xE6, 0x47, 0xD7, 0x60, 0xFA, 0x59, 0xCE, 0x60, 0xFC, 0x5A, 0xDE, 0x6C, 0xF6, 0x58, 0xDC, 0x6E, 0xE5, 0x52, 0xD0, 0x7C, 0xE5, 0x4D, 0xDE, 0x73, 0xFF, 0x52, 0xD3, 0x7C, 0xFC, 0x5D, 0xC0, 0x77, 0xFE, 0x44, 0xC9, 0x6F, 0xE0, 0x5C, 0xC8, 0x71, 0xE4, 0x4E, 0xDD, 0x73, 0xE6, 0x4F, 0xD1, 0x64, 0xE7, 0x54, 0xCC, 0x6A, 0xFE, 0x51, 0xCD, 0x71, 0xE3, 0x4F, 0xD6, 0x61, 0xE0, 0x5B, 0xD5, 0x61, 0xFB, 0x5F, 0xDC, 0x6E, 0xF0, 0x59, 0xD0, 0x69, 0xE6, 0x4D, 0xC0, 0x7D, 0xF0, 0x53, 0xC6, 0x75, 0xF9, 0x41, 0xC1, 0x69, 0xF0, 0x47, 0xC2, 0x62, 0xF8, 0x44, 0xD0, 0x70, 0xE6, 0x5E, 0xC7, 0x6F, 0xFB, 0x42, 0xC9, 0x69, 0xF3, 0x5D, 0xDE, 0x62, 0xFB, 0x40, 0xD2, 0x68, 0xF1, 0x5B, 0xD7, 0x68, 0xE4, 0x55, 0xD0, 0x73, 0xFA, 0x52, 0xC7, 0x71, 0xF1, 0x45, 0xC5, 0x6C, 0xE7, 0x4D, 0xD7, 0x6E, 0xE5, 0x43, 0xCB, 0x62, 0xE0, 0x46, 0xD4, 0x64, 0xE5, 0x4F, 0xC7, 0x60, 0xE6, 0x43, 0xC0, 0x7C, 0xF3, 0x50, 0xDD, 0x77, 0xE2, 0x5F, 0xC7, 0x61, 0xE1, 0x44, 0xCE, 0x6F, 0xFB, 0x46, 0xC9, 0x6F, 0xF7, 0x5C, 0xD4, 0x6C, 0xE5, 0x5A, 0xD1, 0x60, 0xFF, 0x44, 0xDD, 0x6F, 0xF1, 0x4F, 0xDE, 0x6C, 0xFC, 0x54, 0xCD, 0x6B, 0xF3, 0x56, 0xD2, 0x75, 0xE3, 0x5B, 0xCA, 0x7F, 0xFA, 0x50, 0xD7, 0x62, 0xF9, 0x43, 0xC5, 0x6F, 0xF6, 0x41, 0xC7, 0x6B, 0xFE, 0x43, 0xC2, 0x7D, 0xFB, 0x43, 0xC6, 0x73, 0xE1, 0x56, 0xD2, 0x62, 0xFA, 0x4D, 0xCE, 0x61, 0xE2, 0x56, 0xD6, 0x6E, 0xEB, 0x41, 0xDC, 0x63, 0xF3, 0x45, 0xDE, 0x6C, 0xE5, 0x46, 0xC2, 0x77, 0xF3, 0x41, 0xC6, 0x62, 0xE4, 0x4E, 0xC3, 0x7D, 0xF9, 0x44, 0xC3, 0x6D, 0xF9, 0x5B, 0xDE, 0x6E, 0xF8, 0x4F, 0xD1, 0x66, 0xF6, 0x45, 0xD4, 0x74, 0xE5, 0x4D, 0xD3, 0x74, 0xE6, 0x44, 0xDD, 0x66, 0xE7, 0x52, 0xC2, 0x68, 0xEB, 0x53, 0xCC, 0x74, 0xE7, 0x42, 0xC5, 0x63, 0xE2, 0x46, 0xD2, 0x6A, 0xE1, 0x58, 0xDE, 0x7F, 0xE5, 0x54, 0xCB, 0x6F, 0xF4, 0x5B, 0xCE, 0x72, 0xF0, 0x47, 0xC1, 0x77, 0xE6, 0x52, 0xC9, 0x62, 0xF4, 0x5A, 0xC9, 0x62, 0xE2, 0x53, 0xCD, 0x6F, 0xE3, 0x5D, 0xC5, 0x63, 0xF7, 0x5E, 0xDD, 0x60, 0xE6, 0x4D, 0xC2, 0x76, 0xE3, 0x40, 0xC3, 0x6B, 0xE6, 0x5C, 0xD4, 0x60, 0xE3, 0x5D, 0xC8, 0x6E, 0xF7, 0x58, 0xCD, 0x63, 0xF1, 0x43, 0xCE, 0x71, 0xE7, 0x52, 0xD7, 0x72, 0xF9, 0x52, 0xD7, 0x76, 0xE0, 0x4D, 0xDD, 0x70, 0xEB, 0x42, 0xD5, 0x6F, 0xF5, 0x4D, 0xCA, 0x63, 0xFC, 0x53, 0xD0, 0x6D, 0xEA, 0x46, 0xC6, 0x75, 0xF3, 0x44, 0xC7, 0x7F, 0xE3, 0x5A, 0xDC, 0x69, 0xF6, 0x5C, 0xC7, 0x75, 0xE1, 0x40, 0xCA, 0x74, 0xF9, 0x45, 0xC8, 0x6E, 0xEB, 0x4D, 0xDE, 0x61, 0xF5, 0x52, 0xC2, 0x74, 0xFE, 0x5B, 0xDD, 0x70, 0xF1, 0x53, 0xD7, 0x7C, 0xE5, 0x4E, 0xC1, 0x68, 0xE5, 0x52, 0xC2, 0x73, 0xE5, 0x4F, 0xCA, 0x74, 0xE3, 0x54, 0xD3, 0x62, 0xF7, 0x45, 0xD5, 0x67, 0xE6, 0x4D, 0xD0, 0x68, 0xFA, 0x5F, 0xC5, 0x76, 0xFF, 0x5E, 0xC9, 0x6A, 0xF7, 0x47, 0xD1, 0x69, 0xFF, 0x4D, 0xCA, 0x70, 0xE6, 0x53, 0xC3, 0x6C, 0xE0, 0x58, 0xC5, 0x6F, 0xE2, 0x45, 0xD5, 0x69, 0xFF, 0x45, 0xC1, 0x7D, 0xF7, 0x44, 0xC2, 0x6A, 0xF7, 0x59, 0xCE, 0x6A, 0xFE, 0x4E, 0xC8, 0x64, 0xFA, 0x5B, 0xD0, 0x60, 0xF6, 0x40, 0xCC, 0x74, 0xE1, 0x5B, 0xD1, 0x76, 0xFA, 0x45, 0xC0, 0x70, 0xE0, 0x55, 0xC6, 0x6B, 0xF9, 0x4F, 0xC3, 0x70, 0xE7, 0x4D, 0xD4, 0x62, 0xE6, 0x44, 0xD3, 0x76, 0xF1, 0x4D, 0xC6, 0x61, 0xEA, 0x5B, 0xCC, 0x74, 0xF8, 0x58, 0xC1, 0x71, 0xEA, 0x59, 0xCC, 0x6B, 0xF9, 0x47, 0xD3, 0x6E, 0xE5, 0x4E, 0xD4, 0x6E, 0xF7, 0x4D, 0xCF, 0x60, 0xF5, 0x56, 0xD3, 0x64, 0xE2, 0x54, 0xD4, 0x6C, 0xE2, 0x4D, 0xD3, 0x62, 0xE6, 0x5C, 0xC0, 0x72, 0xE6, 0x59, 0xDC, 0x6D, 0xE0, 0x54, 0xD3, 0x60, 0xE4, 0x5A, 0xD3, 0x60, 0xF8, 0x46, 0xDE, 0x7C, 0xF0, 0x54, 0xCF, 0x6C, 0xE1, 0x53, 0xC0, 0x73, 0xEA, 0x4D, 0xDC, 0x69, 0xE1, 0x46, 0xD5, 0x69, 0xE7, 0x43, 0xD6, 0x74, 0xF1, 0x54, 0xC9, 0x61, 0xF7, 0x45, 0xC2, 0x66, 0xF4, 0x5C, 0xDD, 0x7F, 0xE8, 0x4F, 0xC1, 0x74, 0xF3, 0x41, 0xC0, 0x75, 0xF8, 0x55, 0xCE, 0x76, 0xF7, 0x5B, 0xC8, 0x63, 0xE5, 0x5B, 0xCF, 0x75, 0xE8, 0x5D, 0xD3, 0x7C, 0xE3, 0x5F, 0xC1, 0x64, 0xEB, 0x55, 0xD5, 0x68, 0xF3, 0x4E, 0xC8, 0x70, 0xFE, 0x4D, 0xC8, 0x7F, 0xE3, 0x55, 0xC3, 0x64, 0xE6, 0x42, 0xDD, 0x71, 0xF4, 0x4D, 0xC3, 0x71, 0xF3, 0x5D, 0xDE, 0x74, 0xF0, 0x50, 0xC5, 0x76, 0xE3, 0x53, 0xD6, 0x6C, 0xFE, 0x40, 0xD7, 0x62, 0xE8, 0x45, 0xD0, 0x72, 0xF3, 0x5C, 0xDE, 0x7D, 0xF1, 0x41, 0xC2, 0x72, 0xFB, 0x5B, 0xD1, 0x7C, 0xE7, 0x40, 0xC9, 0x77, 0xEB, 0x41, 0xD0, 0x63, 0xE8, 0x44, 0xCD, 0x68, 0xF9, 0x4E, 0xD4, 0x72, 0xF0, 0x45, 0xD0, 0x6A, 0xF4, 0x5C, 0xD0, 0x75, 0xF8, 0x54, 0xCB, 0x63, 0xF3, 0x52, 0xCF, 0x63, 0xFB, 0x43, 0xCA, 0x75, 0xF9, 0x56, 0xCD, 0x61, 0xEA, 0x58, 0xDC, 0x6C, 0xF1, 0x5A, 0xCA, 0x61, 0xF5, 0x5E, 0xD1, 0x74, 0xE0, 0x50, 0xD7, 0x6E, 0xF6, 0x40, 0xCC, 0x72, 0xFA, 0x59, 0xC1, 0x73, 0xFB, 0x54, 0xC1, 0x74, 0xFE, 0x56, 0xC9, 0x64, 0xF8, 0x46, 0xD5, 0x77, 0xFC, 0x5B, 0xCA, 0x7D, 0xFD, 0x5C, 0xDE, 0x76, 0xF8, 0x43, 0xCB, 0x67, 0xF3, 0x53, 0xC9, 0x6B, 0xE6, 0x5A, 0xD3, 0x6F, 0xF9, 0x54, 0xC5, 0x7F, 0xFA, 0x40, 0xD7, 0x62, 0xE5, 0x43, 0xC3, 0x67, 0xE2, 0x56, 0xDC, 0x77, 0xFB, 0x5C, 0xCC, 0x72, 0xFD, 0x5A, 0xC8, 0x6A, 0xFD, 0x53, 0xD4, 0x6D, 0xFD, 0x4E, 0xCC, 0x7D, 0xE6, 0x50, 0xCC, 0x6E, 0xFE, 0x58, 0xD4, 0x77, 0xE1, 0x44, 0xD3, 0x74, 0xFF, 0x59, 0xC9, 0x64, 0xF4, 0x42, 0xC1, 0x6C, 0xF7, 0x56, 0xD2, 0x7C, 0xE3, 0x5B, 0xCF, 0x60, 0xFA, 0x5C, 0xD7 };

        public static byte[][] GetHandShakeFrame(string url, int websocketVersion)
        {
            var address = new Uri(url);

            return new[]
                   {
                       Encoding.UTF8.GetBytes("GET " + address.PathAndQuery),
                       Encoding.UTF8.GetBytes(" HTTP/1.1\r\n"),
                       Encoding.UTF8.GetBytes(string.Format("Host: {0}:{1}", address.Host, address.Port)),                       
                       Encoding.UTF8.GetBytes("\r\nUpgrade:   WebSocket\r\n"),
                       Encoding.UTF8.GetBytes("connection: upgrade\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Origin: http://localhost:80\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Version: "+websocketVersion+"\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Protocol: mywebsocketsubprotocol\r\n"),
                       Encoding.UTF8.GetBytes("\r"),
                       Encoding.UTF8.GetBytes("\n")
                   };
        }


        public static byte[][] GetHandShakeFrameWithAffinityCookie(string url, int websocketVersion, string AffinityCookie)
        {
            var address = new Uri(url);

            return new[]
                   {
                       Encoding.UTF8.GetBytes("GET " + address.PathAndQuery),
                       Encoding.UTF8.GetBytes(" HTTP/1.1\r\n"),
                       Encoding.UTF8.GetBytes(string.Format("Host: {0}:{1}", address.Host, address.Port)),                       
                       Encoding.UTF8.GetBytes("\r\nUpgrade:   WebSocket\r\n"),
                       Encoding.UTF8.GetBytes("connection: upgrade\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Origin: http://localhost:80\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Version: "+websocketVersion+"\r\n"),
                       Encoding.UTF8.GetBytes("Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"),
                       Encoding.UTF8.GetBytes("Cookie: "+AffinityCookie+"\r\n"),
                       Encoding.UTF8.GetBytes("\r"),
                       Encoding.UTF8.GetBytes("\n")
                   };
        }

    }
}
