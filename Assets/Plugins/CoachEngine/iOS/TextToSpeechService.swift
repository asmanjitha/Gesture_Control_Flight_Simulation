//  Copyright © 2019 AGT. All rights reserved.

import AVFoundation

/// Responsible for playing and queuing VoiceOver
class TextToSpeechService: NSObject {
    
    public static let instance = TextToSpeechService()
    
    private let audioSession = AVAudioSession.sharedInstance()
    private static let synthesizer = AVSpeechSynthesizer()
    
    private var audioPlayer: AVAudioPlayer?
    private var immediateAudioPlayer: AVAudioPlayer?
    
    
    private let maxQueueLength = 2
    private var queue: [String] = []
    
    private var isAudioPlayerPlaying: Bool {
        return audioPlayer != nil && audioPlayer!.isPlaying
    }
    
    override init() {
        super.init()
        do {
            try audioSession.setCategory(AVAudioSession.Category.playAndRecord, options: [AVAudioSession.CategoryOptions.defaultToSpeaker])
        } catch {
            NSLog("audioSession properties weren't set because of an error.")
        }
        TextToSpeechService.synthesizer.delegate = self
    }
    
    /// Play the sound. Queue it up if any other sound is playing at the moment (except for the sounds which are played immediate)
    func say(text: String, playImmediate: Bool) {
        DispatchQueue.main.async {
            if playImmediate {
                self.sayNow(text: text, playImmediate: playImmediate)
                return
            }
        
            if self.queue.count >= self.maxQueueLength {
                self.queue.remove(at: 0)
            }
            self.queue.append(text)
            self.sayNext()
        }
    }
    
    /// Play the next queued up sound, if no other sound is playing (except for the sounds which are played immediate)
    private func sayNext() {
        if !isAudioPlayerPlaying && !TextToSpeechService.synthesizer.isSpeaking && queue.count > 0 {
            let text = queue.remove(at: 0)
            sayNow(text: text, playImmediate: true)
        }
    }

    /// Play the provided PlySound.
    /// If there is an mp3-File with the sound.soundId than play the mp3 otherwise use the provided sound.textToSpeech text to use text to speech to play it.
    private func sayNow(text: String, playImmediate: Bool) {
        if (playImmediate) {
            TextToSpeechService.synthesizer.stopSpeaking(at: .immediate)
        }
        utterText(text: text, synthesizer: TextToSpeechService.synthesizer)
    }
    
    /// Generate speech from a provieded text and play it with the provided synthesizer
    private func utterText(text: String, synthesizer: AVSpeechSynthesizer) {
        let utterance = AVSpeechUtterance(string: text)
        utterance.rate = AVSpeechUtteranceDefaultSpeechRate
        utterance.volume = 1
        synthesizer.speak(utterance)
    }
}


// MARK: - AVAudioPlayerDelegate

extension TextToSpeechService: AVAudioPlayerDelegate {
    // on finish play the next in queue
    func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
        sayNext()
    }
}

// MARK: - AVAudioPlayerDelegate

extension TextToSpeechService: AVSpeechSynthesizerDelegate {
    // on finish play the next in queue
    func speechSynthesizer(_ synthesizer: AVSpeechSynthesizer, didFinish utterance: AVSpeechUtterance) {
        sayNext()
    }
}
