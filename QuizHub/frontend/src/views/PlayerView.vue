<script setup>
import { computed, onMounted, ref } from 'vue'
import questions from '../questions.json'
import { useQuizSignalR } from '../useQuizSignalR'

const { connect, connected, error, invoke, on } = useQuizSignalR()
const code = ref(new URLSearchParams(window.location.search).get('code') || '')
const playerName = ref('')
const joined = ref(false)
const answered = ref(false)
const questionActive = ref(false)
const quizEnded = ref(false)
const finalScores = ref([])
const selected = ref(-1)
const currentQuestion = ref(0)
const feedback = ref('')
const revealedPlaces = ref([])
let podiumRevealTimeoutId
const colors = ['red', 'blue', 'yellow', 'green']
const question = computed(() => questions[currentQuestion.value])
const podiumSlots = computed(() => [1, 0, 2].map(index => ({ index, player: finalScores.value[index] })).filter(slot => slot.player))

onMounted(async () => {
  await connect()
  on('QuestionStarted', ({ questionIndex }) => {
    currentQuestion.value = questionIndex
    questionActive.value = true
    answered.value = false
    selected.value = -1
    feedback.value = ''
  })
  on('AnswerAccepted', ({ isCorrect, points }) => {
    answered.value = true
    feedback.value = isCorrect ? `Richtig! +${points}` : 'Nicht ganz. Bleib aufmerksam.'
  })
  on('QuizEnded', scores => {
    finalScores.value = scores
    quizEnded.value = true
    questionActive.value = false
    startPodiumReveal()
  })
})

function startPodiumReveal() {
  revealedPlaces.value = []
  const revealOrder = [2, 1, 0].filter(index => finalScores.value[index])
  let revealIndex = 0
  const revealNext = () => {
    if (revealIndex >= revealOrder.length) return
    revealedPlaces.value.push(revealOrder[revealIndex])
    revealIndex += 1
    if (revealIndex < revealOrder.length) podiumRevealTimeoutId = setTimeout(revealNext, 1500)
  }
  podiumRevealTimeoutId = setTimeout(revealNext, 700)
}

async function join() {
  if (!code.value || !playerName.value.trim()) return
  try {
    await invoke('JoinLobby', code.value, playerName.value)
    joined.value = true
  } catch (exception) {
    feedback.value = exception.message || 'Beitritt fehlgeschlagen.'
  }
}

async function answer(index) {
  if (answered.value || !joined.value) return
  selected.value = index
  if ('vibrate' in navigator) navigator.vibrate(80)
  await invoke('SubmitAnswer', code.value, index)
}

function returnToStart() { window.location.href = window.location.pathname }
</script>

<template>
  <section class="player-shell">
    <div class="brand-mark"><span>FS</span> FAHRSICHER</div>
    <div v-if="!joined" class="join-panel">
      <p class="eyebrow">TEILNEHMERZUGANG</p>
      <h1>Bereit für<br /><em>die nächste Runde?</em></h1>
      <label>Raumcode <input v-model="code" inputmode="numeric" maxlength="4" placeholder="0000" /></label>
      <label>Dein Name <input v-model="playerName" maxlength="40" placeholder="z. B. Alex" @keyup.enter="join" /></label>
      <button class="primary-action wide" :disabled="!connected" @click="join">Beitreten <span>→</span></button>
      <p v-if="error || feedback" class="error-text">{{ error || feedback }}</p>
    </div>
    <div v-else class="player-state">
      <div v-if="quizEnded" class="player-results">
        <p class="eyebrow">QUIZ BEENDET</p>
        <h1>Das Siegerpodest<span>.</span></h1>
        <div class="winner-podium"><div v-for="slot in podiumSlots" :key="slot.player.id" class="winner-slot" :class="[`rank-${slot.index + 1}`, { revealed: revealedPlaces.includes(slot.index) }]" ><span class="winner-name">{{ slot.player.name }}</span><strong>{{ slot.player.score }} P</strong><b>{{ slot.index + 1 }}</b></div></div>
        <button class="primary-action wide" @click="returnToStart">Zur Startseite <span>→</span></button>
      </div>
      <div v-else-if="!questionActive" class="waiting-state">
        <div class="pulse-dot"></div>
        <p class="eyebrow">RAUM {{ code }}</p>
        <h1>Du bist dabei<span>.</span></h1>
        <p>Warte auf den Start der nächsten Frage.</p>
      </div>
      <div v-else class="answer-state">
        <p class="eyebrow">FRAGE {{ currentQuestion + 1 }} / {{ questions.length }}</p>
        <h1 v-if="answered">{{ feedback }}</h1>
        <p v-else>Wähle deine Antwort</p>
        <div class="answer-grid">
          <button v-for="(option, index) in question.options" :key="option" class="answer-button" :class="[colors[index], { selected: selected === index }]" :disabled="answered" @click="answer(index)">
            <span>{{ ['A', 'B', 'C', 'D'][index] }}</span>
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
